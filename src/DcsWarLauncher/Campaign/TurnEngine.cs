using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public sealed class TurnEngine
{
    public WarState Advance(WarState state, BattleReport report)
    {
        state = state.Normalize();

        var bluePressure = Clamp(report.BlueMissionSuccess - report.BlueLosses + report.AirSuperiority);
        var redPressure = Clamp(report.RedMissionSuccess - report.RedLosses - report.AirSuperiority);
        var netPressure = bluePressure - redPressure;

        var supplyDepots = state.SupplyDepots
            .Select(depot => AdvanceSupplyDepot(depot, report))
            .ToList();
        var groundUnits = state.GroundUnits
            .Select(unit => AdvanceGroundUnit(unit, supplyDepots, report))
            .ToList();
        var objectives = state.Objectives
            .Select(objective => AdvanceObjective(objective, groundUnits, netPressure))
            .ToList();
        var airbases = state.Airbases
            .Select(airbase => AdvanceAirbase(airbase, objectives, groundUnits, report))
            .ToList();
        var squadrons = state.Squadrons
            .Select(squadron => AdvanceSquadron(squadron, report))
            .ToList();

        var frontlines = BuildFrontlines(objectives, state.Turn + 1);
        var aiPlan = BuildAiPlan(objectives, airbases, netPressure);
        var missionPackages = BuildMissionPackages(aiPlan, squadrons);
        var now = DateTimeOffset.UtcNow;

        return state with
        {
            Turn = state.Turn + 1,
            Phase = "Planning",
            BlueSupply = Math.Max(0, state.BlueSupply - report.BlueLosses + CapturedBonus(objectives, "blue")),
            RedSupply = Math.Max(0, state.RedSupply - report.RedLosses + CapturedBonus(objectives, "red")),
            Objectives = objectives,
            Airbases = airbases,
            Squadrons = squadrons,
            MissionPackages = missionPackages,
            GroundUnits = groundUnits,
            SupplyDepots = supplyDepots,
            Frontlines = frontlines,
            AiPlan = aiPlan,
            CurrentTurnStartedUtc = now,
            CurrentTurnEndsUtc = now.AddHours(state.TurnDurationHours),
            LastBattleReport = report,
            UpdatedUtc = now
        };
    }

    private static SupplyDepotState AdvanceSupplyDepot(SupplyDepotState depot, BattleReport report)
    {
        var enemySuccess = depot.Coalition == "blue" ? report.RedMissionSuccess : report.BlueMissionSuccess;
        var friendlyLosses = depot.Coalition == "blue" ? report.BlueLosses : report.RedLosses;
        var pressure = Math.Max(0, enemySuccess / 2);
        var stores = Math.Clamp(depot.Stores + 8 - pressure - friendlyLosses / 4, 0, 100);
        var status = stores switch
        {
            <= 20 => "critical",
            <= 45 => "strained",
            _ => pressure > 7 ? "threatened" : "active"
        };

        return depot with { Stores = stores, Status = status };
    }

    private static GroundUnitState AdvanceGroundUnit(GroundUnitState unit, List<SupplyDepotState> depots, BattleReport report)
    {
        var depot = depots
            .Where(candidate => candidate.Coalition == unit.Coalition)
            .OrderByDescending(candidate => candidate.Location == unit.Location)
            .ThenByDescending(candidate => candidate.Stores)
            .FirstOrDefault();
        var losses = unit.Coalition == "blue" ? report.BlueLosses : report.RedLosses;
        var enemySuccess = unit.Coalition == "blue" ? report.RedMissionSuccess : report.BlueMissionSuccess;
        var supplied = depot?.Stores > 35;
        var supplyDelta = supplied ? 4 : -8;
        var postureCost = unit.Posture == "attacking" ? 7 : unit.Posture == "defending" ? 3 : 1;
        var strength = Math.Clamp(unit.Strength - enemySuccess / 3 - losses / 4 + (supplied ? 2 : -2), 0, 100);
        var supply = Math.Clamp(unit.Supply + supplyDelta - postureCost, 0, 100);
        var readiness = Math.Clamp(unit.Readiness + (supplied ? 3 : -5) - losses / 3, 0, 100);
        var posture = supply < 25 || readiness < 30
            ? "reorganizing"
            : unit.Posture == "reserve" && readiness > 70
                ? "attacking"
                : unit.Posture;

        return unit with
        {
            Strength = strength,
            Supply = supply,
            Readiness = readiness,
            Posture = posture
        };
    }

    private static AirbaseState AdvanceAirbase(
        AirbaseState airbase,
        List<ObjectiveState> objectives,
        List<GroundUnitState> groundUnits,
        BattleReport report)
    {
        var matchingObjective = objectives.FirstOrDefault(objective => objective.Name == airbase.Name);
        var localGroundControl = GroundControlAt(airbase.Name, groundUnits);
        var owner = localGroundControl != "contested"
            ? localGroundControl
            : matchingObjective?.Owner == "contested" || matchingObjective is null
            ? airbase.Owner
            : matchingObjective.Owner;
        var pressure = owner switch
        {
            "blue" => report.RedMissionSuccess - report.BlueMissionSuccess,
            "red" => report.BlueMissionSuccess - report.RedMissionSuccess,
            _ => 0
        };
        var runway = Math.Clamp(airbase.RunwayHealth - Math.Max(0, pressure / 2) + 2, 0, 100);
        var fuel = Math.Clamp(airbase.Fuel - Math.Max(0, pressure) + 4, 0, 100);
        var status = runway switch
        {
            <= 25 => "critical",
            <= 55 => "damaged",
            _ => owner == "contested" ? "contested" : "operational"
        };

        return airbase with
        {
            Owner = owner,
            RunwayHealth = runway,
            Fuel = fuel,
            Status = status
        };
    }

    private static SquadronState AdvanceSquadron(SquadronState squadron, BattleReport report)
    {
        var losses = squadron.Coalition == "blue" ? report.BlueLosses : report.RedLosses;
        var repaired = Math.Max(1, squadron.AircraftTotal / 8);
        var ready = Math.Clamp(squadron.AircraftReady - losses / 3 + repaired, 0, squadron.AircraftTotal);
        var readiness = Math.Clamp(squadron.PilotReadiness - losses + 3, 0, 100);

        return squadron with
        {
            AircraftReady = ready,
            PilotReadiness = readiness
        };
    }

    private static ObjectiveState AdvanceObjective(ObjectiveState objective, List<GroundUnitState> groundUnits, int netPressure)
    {
        var localBlueGround = GroundPressureAt(objective.Name, groundUnits, "blue");
        var localRedGround = GroundPressureAt(objective.Name, groundUnits, "red");
        var groundPressure = (localBlueGround - localRedGround) / 8;
        var ownerModifier = objective.Owner switch
        {
            "blue" => 2,
            "red" => -2,
            _ => 0
        };
        var nextStrength = Math.Clamp(objective.Strength + netPressure + groundPressure + ownerModifier, 0, 100);
        var owner = nextStrength switch
        {
            >= 70 => "blue",
            <= 30 => "red",
            _ => "contested"
        };

        return objective with { Owner = owner, Strength = nextStrength };
    }

    private static string GroundControlAt(string location, List<GroundUnitState> groundUnits)
    {
        var blue = GroundPressureAt(location, groundUnits, "blue");
        var red = GroundPressureAt(location, groundUnits, "red");
        if (blue >= red + 25)
        {
            return "blue";
        }

        if (red >= blue + 25)
        {
            return "red";
        }

        return "contested";
    }

    private static int GroundPressureAt(string location, List<GroundUnitState> groundUnits, string coalition) =>
        groundUnits
            .Where(unit => unit.Coalition == coalition && unit.Location == location)
            .Sum(unit =>
            {
                var posture = unit.Posture switch
                {
                    "attacking" => 12,
                    "defending" => 6,
                    "reserve" => 2,
                    _ => -4
                };
                return (unit.Strength + unit.Supply + unit.Readiness) / 3 + posture;
            });

    private static List<FrontlineSegment> BuildFrontlines(List<ObjectiveState> objectives, int turn)
    {
        var segments = new List<FrontlineSegment>();
        for (var i = 0; i < objectives.Count - 1; i++)
        {
            var left = objectives[i];
            var right = objectives[i + 1];
            var control = (left.Strength + right.Strength) / 2;
            var jitter = ((turn + i) % 3 - 1) * 3;
            segments.Add(new FrontlineSegment(
                $"{left.Name}-{right.Name}",
                Math.Clamp(20 + i * 25, 0, 100),
                Math.Clamp(100 - control + jitter, 0, 100),
                Math.Clamp(42 + i * 20, 0, 100),
                Math.Clamp(100 - control - jitter, 0, 100),
                control >= 55 ? "blue-advancing" : control <= 45 ? "red-advancing" : "static"));
        }

        return segments;
    }

    private static List<AiOrder> BuildAiPlan(List<ObjectiveState> objectives, List<AirbaseState> airbases, int netPressure)
    {
        var contested = objectives
            .OrderBy(objective => Math.Abs(50 - objective.Strength))
            .FirstOrDefault();

        if (contested is null)
        {
            return [];
        }

        var blueTask = netPressure >= 0 ? "Exploit breakthrough" : "Rebuild defensive CAP and logistics";
        var redTask = netPressure < 0 ? "Exploit breakthrough" : "Delay enemy advance and strike supply";
        var damagedBlueBase = airbases.FirstOrDefault(airbase => airbase.Owner == "blue" && airbase.RunwayHealth < 60);
        var damagedRedBase = airbases.FirstOrDefault(airbase => airbase.Owner == "red" && airbase.RunwayHealth < 60);

        return
        [
            new AiOrder("blue", damagedBlueBase is null ? blueTask : "Repair runway and protect logistics", damagedBlueBase?.Name ?? contested.Name, Math.Clamp(55 + netPressure, 10, 95)),
            new AiOrder("red", damagedRedBase is null ? redTask : "Repair runway and protect logistics", damagedRedBase?.Name ?? contested.Name, Math.Clamp(55 - netPressure, 10, 95))
        ];
    }

    private static List<MissionPackageState> BuildMissionPackages(List<AiOrder> aiPlan, List<SquadronState> squadrons)
    {
        var packages = new List<MissionPackageState>();
        foreach (var order in aiPlan)
        {
            var squadron = squadrons
                .Where(candidate => candidate.Coalition == order.Coalition && candidate.AircraftReady > 0)
                .OrderByDescending(candidate => candidate.PilotReadiness)
                .FirstOrDefault();

            if (squadron is null)
            {
                continue;
            }

            var task = order.Task.Contains("Repair", StringComparison.OrdinalIgnoreCase)
                ? "CAP"
                : order.Task.Contains("Strike", StringComparison.OrdinalIgnoreCase) || order.Task.Contains("Interdict", StringComparison.OrdinalIgnoreCase)
                    ? "Strike"
                    : "OCA";

            packages.Add(new MissionPackageState(
                $"{order.Coalition.ToUpperInvariant()}-{DateTimeOffset.UtcNow:HHmm}-{packages.Count + 1}",
                order.Coalition,
                task,
                order.Target,
                "Planning",
                Math.Min(4, squadron.AircraftReady),
                squadron.Name));
        }

        return packages;
    }

    private static int CapturedBonus(List<ObjectiveState> objectives, string owner) =>
        objectives.Count(objective => objective.Owner == owner) * 3;

    private static int Clamp(int value) => Math.Clamp(value, -25, 25);
}

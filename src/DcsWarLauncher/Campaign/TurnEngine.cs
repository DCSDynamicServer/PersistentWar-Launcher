using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public sealed class TurnEngine
{
    public WarState Advance(WarState state, BattleReport report)
    {
        state = state.Normalize();

        var pressure = CampaignPressure.From(report);

        var factories = ReinforcementEngine.AdvanceFactories(state.Factories, report);

        var supplyDepots = state.SupplyDepots
            .Select(depot => SupplyEngine.AdvanceDepot(depot, report))
            .ToList();
        supplyDepots = ReinforcementEngine.ApplyFactorySupply(supplyDepots, factories);
        var groundUnits = state.GroundUnits
            .Select(unit => GroundWarEngine.AdvanceUnit(unit, supplyDepots, report))
            .ToList();
        var objectives = state.Objectives
            .Select(objective => ObjectiveEngine.AdvanceObjective(objective, groundUnits, pressure.NetPressure))
            .ToList();
        var airbases = state.Airbases
            .Select(airbase => AirbaseEngine.AdvanceAirbase(airbase, objectives, groundUnits, report))
            .ToList();
        var squadrons = state.Squadrons
            .Select(squadron => SquadronEngine.AdvanceSquadron(squadron, report))
            .ToList();
        squadrons = ReinforcementEngine.ApplyAircraftReplacements(squadrons, factories, supplyDepots);
        groundUnits = ReinforcementEngine.ApplyGroundReinforcements(groundUnits, supplyDepots, factories);

        var frontlines = FrontlineEngine.BuildFrontlines(objectives, state.Turn + 1);
        var aiPlan = PlanningEngine.BuildAiPlan(objectives, airbases, pressure.NetPressure);
        var missionPackages = PlanningEngine.BuildMissionPackages(aiPlan, squadrons);
        var now = DateTimeOffset.UtcNow;

        return state with
        {
            Turn = state.Turn + 1,
            Phase = "Planning",
            BlueSupply = SupplyEngine.AdvanceCoalitionSupply(state.BlueSupply, report.BlueLosses, objectives, "blue"),
            RedSupply = SupplyEngine.AdvanceCoalitionSupply(state.RedSupply, report.RedLosses, objectives, "red"),
            Objectives = objectives,
            Airbases = airbases,
            Squadrons = squadrons,
            MissionPackages = missionPackages,
            GroundUnits = groundUnits,
            SupplyDepots = supplyDepots,
            Factories = factories,
            Frontlines = frontlines,
            AiPlan = aiPlan,
            CurrentTurnStartedUtc = now,
            CurrentTurnEndsUtc = now.AddHours(state.TurnDurationHours),
            LastBattleReport = report,
            UpdatedUtc = now
        };
    }
}

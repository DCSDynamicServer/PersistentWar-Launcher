using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public static class GroundWarEngine
{
    public static GroundUnitState AdvanceUnit(
        GroundUnitState unit,
        IReadOnlyCollection<SupplyDepotState> depots,
        BattleReport report)
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
        var posture = strength < 30 || supply < 25 || readiness < 30
            ? "reorganizing"
            : strength < 45 && unit.Posture == "attacking"
                ? "defending"
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

    public static string GroundControlAt(string location, IReadOnlyCollection<GroundUnitState> groundUnits)
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

    public static int GroundPressureAt(
        string location,
        IReadOnlyCollection<GroundUnitState> groundUnits,
        string coalition)
    {
        return groundUnits
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
    }
}

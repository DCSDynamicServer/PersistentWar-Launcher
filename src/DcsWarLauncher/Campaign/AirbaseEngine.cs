using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public static class AirbaseEngine
{
    public static AirbaseState AdvanceAirbase(
        AirbaseState airbase,
        IReadOnlyCollection<ObjectiveState> objectives,
        IReadOnlyCollection<GroundUnitState> groundUnits,
        BattleReport report)
    {
        var matchingObjective = objectives.FirstOrDefault(objective => objective.Name == airbase.Name);
        var localGroundControl = GroundWarEngine.GroundControlAt(airbase.Name, groundUnits);
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
}

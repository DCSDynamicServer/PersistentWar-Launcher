using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public static class SupplyEngine
{
    public static SupplyDepotState AdvanceDepot(SupplyDepotState depot, BattleReport report)
    {
        var enemySuccess = depot.Coalition == "blue" ? report.RedMissionSuccess : report.BlueMissionSuccess;
        var friendlyLosses = depot.Coalition == "blue" ? report.BlueLosses : report.RedLosses;
        var pressure = Math.Max(0, enemySuccess / 2);
        var stores = Math.Clamp(depot.Stores - pressure - friendlyLosses / 4, 0, 100);
        var status = stores switch
        {
            <= 20 => "critical",
            <= 45 => "strained",
            _ => pressure > 7 ? "threatened" : "active"
        };

        return depot with { Stores = stores, Status = status };
    }

    public static int AdvanceCoalitionSupply(
        int currentSupply,
        int losses,
        IReadOnlyCollection<ObjectiveState> objectives,
        string owner)
    {
        return Math.Max(0, currentSupply - losses + CapturedBonus(objectives, owner));
    }

    private static int CapturedBonus(IReadOnlyCollection<ObjectiveState> objectives, string owner) =>
        objectives.Count(objective => objective.Owner == owner) * 3;
}

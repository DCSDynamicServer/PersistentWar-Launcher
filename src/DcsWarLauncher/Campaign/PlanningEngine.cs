using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public static class PlanningEngine
{
    public static List<AiOrder> BuildAiPlan(
        IReadOnlyCollection<ObjectiveState> objectives,
        IReadOnlyCollection<AirbaseState> airbases,
        int netPressure)
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

    public static List<MissionPackageState> BuildMissionPackages(
        IReadOnlyCollection<AiOrder> aiPlan,
        IReadOnlyCollection<SquadronState> squadrons)
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
}

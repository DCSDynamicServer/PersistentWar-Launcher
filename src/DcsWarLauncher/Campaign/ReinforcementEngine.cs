using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public static class ReinforcementEngine
{
    public static List<FactoryState> AdvanceFactories(
        IReadOnlyCollection<FactoryState> factories,
        BattleReport report)
    {
        return factories
            .Select(factory => AdvanceFactory(factory, report))
            .ToList();
    }

    public static List<SupplyDepotState> ApplyFactorySupply(
        IReadOnlyCollection<SupplyDepotState> depots,
        IReadOnlyCollection<FactoryState> factories)
    {
        return depots
            .Select(depot =>
            {
                var supplyProduction = ProductionFor(factories, depot.Coalition, "supply");
                var groundProduction = ProductionFor(factories, depot.Coalition, "ground");
                var generalProduction = groundProduction > 0 ? Math.Max(1, groundProduction / 2) : 0;
                var stores = Math.Clamp(depot.Stores + supplyProduction + generalProduction, 0, 100);
                var status = stores switch
                {
                    <= 20 => "critical",
                    <= 45 => "strained",
                    _ => depot.Status == "critical" ? "strained" : depot.Status
                };

                return depot with { Stores = stores, Status = status };
            })
            .ToList();
    }

    public static List<SquadronState> ApplyAircraftReplacements(
        IReadOnlyCollection<SquadronState> squadrons,
        IReadOnlyCollection<FactoryState> factories,
        IReadOnlyCollection<SupplyDepotState> depots)
    {
        var remainingByCoalition = factories
            .GroupBy(factory => factory.Coalition)
            .ToDictionary(
                group => group.Key,
                group => ProductionFor(group, group.Key, "aircraft"));
        var hasAircraftLogistics = depots
            .GroupBy(depot => depot.Coalition)
            .ToDictionary(
                group => group.Key,
                group => group.Any(depot => depot.Stores >= 35));

        return squadrons
            .Select(squadron =>
            {
                var logisticsReady = hasAircraftLogistics.GetValueOrDefault(squadron.Coalition);
                var replacements = logisticsReady ? remainingByCoalition.GetValueOrDefault(squadron.Coalition) : 0;
                var needed = Math.Max(0, squadron.AircraftTotal - squadron.AircraftReady);
                var applied = Math.Min(needed, replacements);
                remainingByCoalition[squadron.Coalition] = Math.Max(0, replacements - applied);

                return squadron with
                {
                    AircraftReady = Math.Clamp(squadron.AircraftReady + applied, 0, squadron.AircraftTotal),
                    PilotReadiness = Math.Clamp(squadron.PilotReadiness + (applied > 0 ? 2 : logisticsReady ? 1 : 0), 0, 100)
                };
            })
            .ToList();
    }

    public static List<GroundUnitState> ApplyGroundReinforcements(
        IReadOnlyCollection<GroundUnitState> groundUnits,
        IReadOnlyCollection<SupplyDepotState> depots,
        IReadOnlyCollection<FactoryState> factories)
    {
        return groundUnits
            .Select(unit =>
            {
                var depot = depots
                    .Where(candidate => candidate.Coalition == unit.Coalition)
                    .OrderByDescending(candidate => candidate.Location == unit.Location)
                    .ThenByDescending(candidate => candidate.Stores)
                    .FirstOrDefault();
                var supplied = depot?.Stores >= 45;
                var production = ProductionFor(factories, unit.Coalition, "ground");
                var strengthGain = supplied ? Math.Max(1, production / 2) : 0;
                var readinessGain = supplied ? 5 : 1;
                var posture = unit.Posture == "reorganizing" && unit.Strength + strengthGain >= 35 && unit.Readiness + readinessGain >= 35
                    ? "defending"
                    : unit.Posture;

                return unit with
                {
                    Strength = Math.Clamp(unit.Strength + strengthGain, 0, 100),
                    Readiness = Math.Clamp(unit.Readiness + readinessGain, 0, 100),
                    Posture = posture
                };
            })
            .ToList();
    }

    private static FactoryState AdvanceFactory(FactoryState factory, BattleReport report)
    {
        var enemySuccess = factory.Coalition == "blue" ? report.RedMissionSuccess : report.BlueMissionSuccess;
        var damage = Math.Max(0, enemySuccess / 5);
        var health = Math.Clamp(factory.Health - damage + 2, 0, 100);
        var status = health switch
        {
            <= 20 => "offline",
            <= 50 => "damaged",
            _ => "active"
        };
        var production = status == "offline"
            ? 0
            : Math.Max(1, NominalProduction(factory) * health / 100);

        return factory with
        {
            Health = health,
            Production = production,
            Status = status
        };
    }

    private static int ProductionFor(IEnumerable<FactoryState> factories, string coalition, string outputType)
    {
        return factories
            .Where(factory => factory.Coalition == coalition && factory.OutputType == outputType && factory.Status != "offline")
            .Sum(factory => factory.Status == "damaged" ? Math.Max(1, factory.Production / 2) : factory.Production);
    }

    private static int NominalProduction(FactoryState factory) =>
        factory.OutputType switch
        {
            "aircraft" => Math.Max(factory.Production, 3),
            "ground" => Math.Max(factory.Production, 5),
            "supply" => Math.Max(factory.Production, 4),
            _ => Math.Max(factory.Production, 1)
        };
}

namespace DcsWarLauncher.Domain;

public sealed record WarState(
    string Theater,
    int Turn,
    string Phase,
    int TurnDurationHours,
    DateTimeOffset CurrentTurnStartedUtc,
    DateTimeOffset CurrentTurnEndsUtc,
    int BlueSupply,
    int RedSupply,
    List<ObjectiveState> Objectives,
    List<AirbaseState> Airbases,
    List<SquadronState> Squadrons,
    List<MissionPackageState> MissionPackages,
    List<GroundUnitState> GroundUnits,
    List<SupplyDepotState> SupplyDepots,
    List<FactoryState> Factories,
    List<FrontlineSegment> Frontlines,
    List<AiOrder> AiPlan,
    BattleReport? LastBattleReport,
    DateTimeOffset UpdatedUtc)
{
    public static WarState CreateDefault()
    {
        var now = DateTimeOffset.UtcNow;
        var objectives = new List<ObjectiveState>
        {
            new("Gudauta", "blue", 65),
            new("Sukhumi", "contested", 48),
            new("Senaki", "red", 35),
            new("Kutaisi", "red", 25)
        };
        var airbases = new List<AirbaseState>
        {
            new("Gudauta", "blue", 82, 74, 18, 20, "operational"),
            new("Sukhumi", "contested", 46, 39, 38, 42, "damaged"),
            new("Senaki", "red", 58, 66, 60, 58, "operational"),
            new("Kutaisi", "red", 91, 84, 78, 70, "fortified")
        };
        var squadrons = new List<SquadronState>
        {
            new("11th Fighter Squadron", "blue", "F-16C", "Gudauta", 14, 10, 76),
            new("336th Strike Squadron", "blue", "F/A-18C", "Gudauta", 12, 8, 68),
            new("159th Guards Fighter", "red", "Su-27", "Kutaisi", 16, 12, 72),
            new("368th Attack Regiment", "red", "Su-25T", "Senaki", 14, 9, 64)
        };
        var packages = new List<MissionPackageState>
        {
            new("BLUE-101", "blue", "BARCAP", "Sukhumi", "Ready", 4, "11th Fighter Squadron"),
            new("BLUE-204", "blue", "Strike", "Senaki Depot", "Planning", 6, "336th Strike Squadron"),
            new("RED-144", "red", "Intercept", "Sukhumi", "Ready", 4, "159th Guards Fighter"),
            new("RED-305", "red", "CAS", "Sukhumi Front", "Planning", 4, "368th Attack Regiment")
        };
        var groundUnits = new List<GroundUnitState>
        {
            new("1st Blue Armored", "blue", "armor", "Sukhumi", 78, 68, 54, "attacking"),
            new("2nd Blue Mechanized", "blue", "mechanized", "Gudauta", 86, 80, 62, "reserve"),
            new("31st Red Guards", "red", "armor", "Senaki", 74, 72, 58, "defending"),
            new("45th Red Motor Rifle", "red", "mechanized", "Sukhumi", 69, 65, 49, "attacking")
        };
        var supplyDepots = new List<SupplyDepotState>
        {
            new("Gudauta Depot", "blue", "Gudauta", 80, 18, 28, "active"),
            new("Sukhumi Forward Depot", "blue", "Sukhumi", 42, 36, 48, "threatened"),
            new("Senaki Depot", "red", "Senaki", 76, 61, 64, "active"),
            new("Kutaisi Depot", "red", "Kutaisi", 92, 79, 77, "active")
        };
        var factories = new List<FactoryState>
        {
            new("Blue Aircraft Works", "blue", "Gudauta", "aircraft", 70, 3, "active"),
            new("Blue Army Depot", "blue", "Gudauta", "ground", 62, 5, "active"),
            new("Red Aviation Plant", "red", "Kutaisi", "aircraft", 75, 3, "active"),
            new("Red Vehicle Plant", "red", "Kutaisi", "ground", 68, 5, "active")
        };

        return new WarState(
            "Caucasus",
            1,
            "Planning",
            6,
            now,
            now.AddHours(6),
            100,
            100,
            objectives,
            airbases,
            squadrons,
            packages,
            groundUnits,
            supplyDepots,
            factories,
            [
                new FrontlineSegment("Gudauta-Sukhumi", 22, 42, 45, 48, "static"),
                new FrontlineSegment("Sukhumi-Senaki", 47, 52, 68, 62, "red-advancing")
            ],
            [
                new AiOrder("blue", "Build pressure on contested airfields", "Sukhumi", 62),
                new AiOrder("red", "Interdict blue supply routes", "Senaki", 58)
            ],
            null,
            now);
    }

    public WarState Normalize()
    {
        var fallback = CreateDefault();
        var duration = TurnDurationHours <= 0 ? 6 : TurnDurationHours;
        var started = CurrentTurnStartedUtc == default ? UpdatedUtc : CurrentTurnStartedUtc;
        if (started == default)
        {
            started = DateTimeOffset.UtcNow;
        }

        return this with
        {
            TurnDurationHours = duration,
            CurrentTurnStartedUtc = started,
            CurrentTurnEndsUtc = CurrentTurnEndsUtc == default ? started.AddHours(duration) : CurrentTurnEndsUtc,
            Objectives = Objectives is { Count: > 0 } ? Objectives : fallback.Objectives,
            Airbases = Airbases is { Count: > 0 } ? Airbases : fallback.Airbases,
            Squadrons = Squadrons is { Count: > 0 } ? Squadrons : fallback.Squadrons,
            MissionPackages = MissionPackages ?? fallback.MissionPackages,
            GroundUnits = GroundUnits is { Count: > 0 } ? GroundUnits : fallback.GroundUnits,
            SupplyDepots = SupplyDepots is { Count: > 0 } ? SupplyDepots : fallback.SupplyDepots,
            Factories = Factories is { Count: > 0 } ? Factories : fallback.Factories,
            Frontlines = Frontlines is { Count: > 0 } ? Frontlines : fallback.Frontlines,
            AiPlan = AiPlan ?? fallback.AiPlan
        };
    }
}

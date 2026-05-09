namespace DcsWarLauncher.Domain;

public sealed record WarState(
    string CampaignId,
    string CampaignName,
    string Theater,
    int SchemaVersion,
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
    List<TurnHistoryEntry> TurnHistory,
    BattleReport? LastBattleReport,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc)
{
    public const int CurrentSchemaVersion = 1;

    public static WarState CreateDefault()
    {
        var now = DateTimeOffset.UtcNow;
        var objectives = new List<ObjectiveState>
        {
            new("Kutaisi", "blue", 72),
            new("Senaki", "contested", 52),
            new("Sukhumi", "contested", 45),
            new("Krasnodar Center", "red", 78)
        };
        var airbases = new List<AirbaseState>
        {
            new("Kutaisi", "blue", 91, 84, 78, 70, "fortified"),
            new("Senaki", "contested", 58, 66, 60, 58, "operational"),
            new("Sukhumi", "contested", 46, 39, 38, 42, "damaged"),
            new("Krasnodar Center", "red", 88, 86, 82, 18, "fortified")
        };
        var squadrons = new List<SquadronState>
        {
            new("Georgian/NATO 11th Fighter Squadron", "blue", "F-16C", "Kutaisi", 14, 10, 76),
            new("Georgian/NATO 336th Strike Squadron", "blue", "F/A-18C", "Kutaisi", 12, 8, 68),
            new("Russian 159th Guards Fighter", "red", "Su-27", "Krasnodar Center", 16, 12, 72),
            new("Russian 368th Attack Regiment", "red", "Su-25T", "Krasnodar Center", 14, 9, 64)
        };
        var packages = new List<MissionPackageState>
        {
            new("BLUE-101", "blue", "BARCAP", "Senaki", "Ready", 4, "Georgian/NATO 11th Fighter Squadron"),
            new("BLUE-204", "blue", "Strike", "Krasnodar Forward Depot", "Planning", 6, "Georgian/NATO 336th Strike Squadron"),
            new("RED-144", "red", "Intercept", "Senaki", "Ready", 4, "Russian 159th Guards Fighter"),
            new("RED-305", "red", "CAS", "Senaki Front", "Planning", 4, "Russian 368th Attack Regiment")
        };
        var groundUnits = new List<GroundUnitState>
        {
            new("1st Georgian Armored", "blue", "armor", "Senaki", 78, 68, 54, "attacking"),
            new("2nd NATO Mechanized", "blue", "mechanized", "Kutaisi", 86, 80, 62, "reserve"),
            new("31st Russian Guards", "red", "armor", "Sukhumi", 74, 72, 58, "defending"),
            new("45th Russian Motor Rifle", "red", "mechanized", "Senaki", 69, 65, 49, "attacking")
        };
        var supplyDepots = new List<SupplyDepotState>
        {
            new("Kutaisi Depot", "blue", "Kutaisi", 92, 79, 77, "active"),
            new("Senaki Forward Depot", "blue", "Senaki", 42, 58, 64, "threatened"),
            new("Krasnodar Center Depot", "red", "Krasnodar Center", 88, 82, 25, "active"),
            new("Sukhumi Forward Depot", "red", "Sukhumi", 68, 38, 48, "threatened")
        };
        var factories = new List<FactoryState>
        {
            new("Kutaisi Aircraft Works", "blue", "Kutaisi", "aircraft", 70, 3, "active"),
            new("Kutaisi Army Depot", "blue", "Kutaisi", "ground", 62, 5, "active"),
            new("Krasnodar Aviation Plant", "red", "Krasnodar Center", "aircraft", 75, 3, "active"),
            new("Krasnodar Vehicle Plant", "red", "Krasnodar Center", "ground", 68, 5, "active")
        };

        return new WarState(
            $"campaign-{now:yyyyMMddHHmmss}",
            "Russia vs Georgia/NATO",
            "Caucasus",
            CurrentSchemaVersion,
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
                new FrontlineSegment("Kutaisi-Senaki", 78, 70, 60, 58, "blue-advancing"),
                new FrontlineSegment("Senaki-Sukhumi", 60, 58, 38, 42, "static"),
                new FrontlineSegment("Sukhumi-Krasnodar", 38, 42, 82, 18, "red-advancing")
            ],
            [
                new AiOrder("blue", "Build pressure north from Kutaisi", "Senaki", 62),
                new AiOrder("red", "Push south from Krasnodar and strike supply", "Senaki", 58)
            ],
            [],
            null,
            now,
            now);
    }

    public WarState Normalize()
    {
        var fallback = CreateDefault();
        var duration = TurnDurationHours <= 0 ? 6 : TurnDurationHours;
        var created = CreatedUtc == default ? UpdatedUtc : CreatedUtc;
        if (created == default)
        {
            created = DateTimeOffset.UtcNow;
        }

        var started = CurrentTurnStartedUtc == default ? UpdatedUtc : CurrentTurnStartedUtc;
        if (started == default)
        {
            started = DateTimeOffset.UtcNow;
        }

        return this with
        {
            CampaignId = string.IsNullOrWhiteSpace(CampaignId) ? fallback.CampaignId : CampaignId,
            CampaignName = string.IsNullOrWhiteSpace(CampaignName) ? fallback.CampaignName : CampaignName,
            Theater = string.IsNullOrWhiteSpace(Theater) ? fallback.Theater : Theater,
            SchemaVersion = CurrentSchemaVersion,
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
            AiPlan = AiPlan ?? fallback.AiPlan,
            TurnHistory = TurnHistory ?? [],
            CreatedUtc = created,
            UpdatedUtc = UpdatedUtc == default ? DateTimeOffset.UtcNow : UpdatedUtc
        };
    }
}

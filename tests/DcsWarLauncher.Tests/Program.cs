using DcsWarLauncher.Campaign;
using DcsWarLauncher.Domain;
using DcsWarLauncher.Infrastructure;
using DcsWarLauncher.Mission;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO.Compression;
using System.Text.Json;

var tests = new (string Name, Action Test)[]
{
    ("Supply depot loses stores under enemy pressure", SupplyDepotLosesStoresUnderEnemyPressure),
    ("Undersupplied ground unit reorganizes", UndersuppliedGroundUnitReorganizes),
    ("Weak attacking ground unit falls back", WeakAttackingGroundUnitFallsBack),
    ("Ground pressure can capture objective", GroundPressureCanCaptureObjective),
    ("Ground control can capture airbase", GroundControlCanCaptureAirbase),
    ("Squadron engine does not repair aircraft", SquadronEngineDoesNotRepairAircraft),
    ("Turn engine advances full state", TurnEngineAdvancesFullState),
    ("Turn engine creates no packages without ready aircraft", TurnCreatesNoPackagesWithoutReadyAircraft),
    ("Normalize preserves empty mission packages", NormalizePreservesEmptyMissionPackages),
    ("Planning engine skips packages without aircraft", PlanningSkipsPackagesWithoutAircraft),
    ("Factories recover aircraft readiness", FactoriesRecoverAircraftReadiness),
    ("Critical depots block aircraft recovery", CriticalDepotsBlockAircraftRecovery),
    ("Damaged factories provide limited aircraft recovery", DamagedFactoriesProvideLimitedAircraftRecovery),
    ("Offline factories do not restock critical depots", OfflineFactoriesDoNotRestockCriticalDepots),
    ("Damaged ground factories slowly restock depots", DamagedGroundFactoriesSlowlyRestockDepots),
    ("Factories reinforce supplied ground units", FactoriesReinforceSuppliedGroundUnits),
    ("Turn engine keeps factories populated", TurnEngineKeepsFactoriesPopulated),
    ("Normalize upgrades war state schema", NormalizeUpgradesWarStateSchema),
    ("Normalize fills campaign metadata", NormalizeFillsCampaignMetadata),
    ("Turn engine preserves campaign metadata", TurnEnginePreservesCampaignMetadata),
    ("State store creates backup before overwrite", StateStoreCreatesBackupBeforeOverwrite),
    ("State store recovers corrupt save", StateStoreRecoversCorruptSave),
    ("State store prunes old backups", StateStorePrunesOldBackups),
    ("Turn engine appends battle report history", TurnEngineAppendsBattleReportHistory),
    ("Turn history keeps latest twenty entries", TurnHistoryKeepsLatestTwentyEntries),
    ("Mission plan exporter writes campaign plan", MissionPlanExporterWritesCampaignPlan),
    ("Mission plan routes packages through anchors", MissionPlanRoutesPackagesThroughAnchors),
    ("Mission plan exporter prepares mission copy", MissionPlanExporterPreparesMissionCopy),
    ("Mission template inspector reports WL anchors", MissionTemplateInspectorReportsWlAnchors),
    ("Mission template inspector reports missing template", MissionTemplateInspectorReportsMissingTemplate)
};

var failures = new List<string>();
foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"FAIL {name}");
        Console.WriteLine(ex.Message);
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"{failures.Count} test(s) failed.");
    Environment.Exit(1);
}

Console.WriteLine();
Console.WriteLine($"{tests.Length} tests passed.");

static void SupplyDepotLosesStoresUnderEnemyPressure()
{
    var depot = new SupplyDepotState("Gudauta Depot", "blue", "Gudauta", 40, 10, 10, "active");
    var report = new BattleReport(0, 30, 4, 0, -5);

    var result = SupplyEngine.AdvanceDepot(depot, report);

    Assert.Equal(24, result.Stores);
    Assert.Equal("strained", result.Status);
}

static void UndersuppliedGroundUnitReorganizes()
{
    var unit = new GroundUnitState("1st Blue Armored", "blue", "armor", "Sukhumi", 40, 20, 28, "attacking");
    var depots = new[]
    {
        new SupplyDepotState("Gudauta Depot", "blue", "Gudauta", 10, 10, 10, "critical")
    };
    var report = new BattleReport(0, 12, 3, 0, -2);

    var result = GroundWarEngine.AdvanceUnit(unit, depots, report);

    Assert.Equal("reorganizing", result.Posture);
    Assert.True(result.Supply < unit.Supply, "Expected supply to decrease.");
    Assert.True(result.Readiness < unit.Readiness, "Expected readiness to decrease.");
}

static void WeakAttackingGroundUnitFallsBack()
{
    var unit = new GroundUnitState("45th Red Motor Rifle", "red", "mechanized", "Sukhumi", 38, 80, 70, "attacking");
    var depots = new[]
    {
        new SupplyDepotState("Senaki Depot", "red", "Senaki", 90, 60, 60, "active")
    };
    var report = new BattleReport(0, 0, 0, 0, 0);

    var result = GroundWarEngine.AdvanceUnit(unit, depots, report);

    Assert.Equal("defending", result.Posture);
}

static void GroundPressureCanCaptureObjective()
{
    var objective = new ObjectiveState("Sukhumi", "contested", 52);
    var groundUnits = new[]
    {
        new GroundUnitState("Blue Armor", "blue", "armor", "Sukhumi", 100, 100, 100, "attacking")
    };

    var result = ObjectiveEngine.AdvanceObjective(objective, groundUnits, 8);

    Assert.Equal("blue", result.Owner);
    Assert.True(result.Strength >= 70, "Expected blue objective strength threshold.");
}

static void GroundControlCanCaptureAirbase()
{
    var airbase = new AirbaseState("Senaki", "red", 60, 50, 50, 50, "operational");
    var objectives = new[]
    {
        new ObjectiveState("Senaki", "contested", 55)
    };
    var groundUnits = new[]
    {
        new GroundUnitState("Blue Armor", "blue", "armor", "Senaki", 100, 100, 100, "attacking")
    };
    var report = new BattleReport(8, 4, 1, 3, 2);

    var result = AirbaseEngine.AdvanceAirbase(airbase, objectives, groundUnits, report);

    Assert.Equal("blue", result.Owner);
    Assert.Equal("operational", result.Status);
}

static void SquadronEngineDoesNotRepairAircraft()
{
    var squadron = new SquadronState("11th Fighter Squadron", "blue", "F-16C", "Gudauta", 14, 5, 50);
    var report = new BattleReport(0, 0, 0, 0, 0);

    var result = SquadronEngine.AdvanceSquadron(squadron, report);

    Assert.Equal(5, result.AircraftReady);
    Assert.Equal(50, result.PilotReadiness);
}

static void TurnEngineAdvancesFullState()
{
    var state = WarState.CreateDefault();
    var report = new BattleReport(12, 7, 3, 8, 4);

    var result = new TurnEngine().Advance(state, report);

    Assert.Equal(state.Turn + 1, result.Turn);
    Assert.Equal(6, result.TurnDurationHours);
    Assert.Equal(report, result.LastBattleReport);
    Assert.NotEmpty(result.Objectives, "Objectives should remain populated.");
    Assert.NotEmpty(result.Airbases, "Airbases should remain populated.");
    Assert.NotEmpty(result.GroundUnits, "Ground units should remain populated.");
    Assert.NotEmpty(result.SupplyDepots, "Supply depots should remain populated.");
    Assert.NotEmpty(result.MissionPackages, "Mission packages should be planned.");
    Assert.True(result.CurrentTurnEndsUtc > result.CurrentTurnStartedUtc, "Turn end should be after start.");
}

static void PlanningSkipsPackagesWithoutAircraft()
{
    var aiPlan = new[]
    {
        new AiOrder("blue", "Exploit breakthrough", "Sukhumi", 70)
    };
    var squadrons = new[]
    {
        new SquadronState("Empty Squadron", "blue", "F-16C", "Gudauta", 12, 0, 90)
    };

    var packages = PlanningEngine.BuildMissionPackages(aiPlan, squadrons);

    Assert.Empty(packages, "No ready aircraft should mean no package.");
}

static void TurnCreatesNoPackagesWithoutReadyAircraft()
{
    var state = WarState.CreateDefault() with
    {
        Squadrons = WarState.CreateDefault().Squadrons
            .Select(squadron => squadron with { AircraftReady = 0, PilotReadiness = 0 })
            .ToList(),
        Factories = WarState.CreateDefault().Factories
            .Select(factory => factory.OutputType == "aircraft"
                ? factory with { Health = 0, Production = 0, Status = "offline" }
                : factory)
            .ToList()
    };
    var report = new BattleReport(25, 25, 50, 50, 0);

    var result = new TurnEngine().Advance(state, report);

    Assert.Empty(result.MissionPackages, "No ready aircraft after turn should mean no packages.");
}

static void NormalizePreservesEmptyMissionPackages()
{
    var state = WarState.CreateDefault() with { MissionPackages = [] };

    var normalized = state.Normalize();

    Assert.Empty(normalized.MissionPackages, "An empty package plan is valid and should not be replaced by defaults.");
}

static void FactoriesRecoverAircraftReadiness()
{
    var squadrons = new[]
    {
        new SquadronState("11th Fighter Squadron", "blue", "F-16C", "Gudauta", 14, 0, 0)
    };
    var factories = new[]
    {
        new FactoryState("Blue Aircraft Works", "blue", "Gudauta", "aircraft", 100, 3, "active")
    };
    var depots = new[]
    {
        new SupplyDepotState("Gudauta Depot", "blue", "Gudauta", 60, 18, 28, "active")
    };

    var result = ReinforcementEngine.ApplyAircraftReplacements(squadrons, factories, depots);

    Assert.Equal(3, result[0].AircraftReady);
    Assert.True(result[0].PilotReadiness > 0, "Expected pilot readiness to recover.");
}

static void CriticalDepotsBlockAircraftRecovery()
{
    var squadrons = new[]
    {
        new SquadronState("11th Fighter Squadron", "blue", "F-16C", "Gudauta", 14, 0, 0)
    };
    var factories = new[]
    {
        new FactoryState("Blue Aircraft Works", "blue", "Gudauta", "aircraft", 100, 3, "active")
    };
    var depots = new[]
    {
        new SupplyDepotState("Gudauta Depot", "blue", "Gudauta", 20, 18, 28, "critical")
    };

    var result = ReinforcementEngine.ApplyAircraftReplacements(squadrons, factories, depots);

    Assert.Equal(0, result[0].AircraftReady);
    Assert.Equal(0, result[0].PilotReadiness);
}

static void DamagedFactoriesProvideLimitedAircraftRecovery()
{
    var squadrons = new[]
    {
        new SquadronState("11th Fighter Squadron", "blue", "F-16C", "Gudauta", 14, 0, 0)
    };
    var factories = new[]
    {
        new FactoryState("Blue Aircraft Works", "blue", "Gudauta", "aircraft", 40, 3, "damaged")
    };
    var depots = new[]
    {
        new SupplyDepotState("Gudauta Depot", "blue", "Gudauta", 60, 18, 28, "active")
    };

    var result = ReinforcementEngine.ApplyAircraftReplacements(squadrons, factories, depots);

    Assert.Equal(1, result[0].AircraftReady);
    Assert.Equal(2, result[0].PilotReadiness);
}

static void OfflineFactoriesDoNotRestockCriticalDepots()
{
    var state = WarState.CreateDefault() with
    {
        SupplyDepots = WarState.CreateDefault().SupplyDepots
            .Select(depot => depot with { Stores = 0, Status = "critical" })
            .ToList(),
        Factories = WarState.CreateDefault().Factories
            .Select(factory => factory with { Health = 10, Production = 0, Status = "offline" })
            .ToList()
    };

    var result = new TurnEngine().Advance(state, new BattleReport(0, 0, 0, 0, 0));

    Assert.True(result.SupplyDepots.All(depot => depot.Stores == 0), "Offline factories should not create depot stores.");
}

static void DamagedGroundFactoriesSlowlyRestockDepots()
{
    var depots = new[]
    {
        new SupplyDepotState("Gudauta Depot", "blue", "Gudauta", 0, 18, 28, "critical")
    };
    var factories = new[]
    {
        new FactoryState("Blue Army Depot", "blue", "Gudauta", "ground", 35, 1, "damaged")
    };

    var result = ReinforcementEngine.ApplyFactorySupply(depots, factories);

    Assert.Equal(1, result[0].Stores);
    Assert.Equal("critical", result[0].Status);
}

static void FactoriesReinforceSuppliedGroundUnits()
{
    var units = new[]
    {
        new GroundUnitState("1st Blue Armored", "blue", "armor", "Sukhumi", 20, 60, 32, "reorganizing")
    };
    var depots = new[]
    {
        new SupplyDepotState("Sukhumi Forward Depot", "blue", "Sukhumi", 60, 36, 48, "active")
    };
    var factories = new[]
    {
        new FactoryState("Blue Army Depot", "blue", "Gudauta", "ground", 100, 6, "active")
    };

    var result = ReinforcementEngine.ApplyGroundReinforcements(units, depots, factories);

    Assert.True(result[0].Strength > units[0].Strength, "Expected strength to recover.");
    Assert.True(result[0].Readiness > units[0].Readiness, "Expected readiness to recover.");
}

static void TurnEngineKeepsFactoriesPopulated()
{
    var state = WarState.CreateDefault();
    var report = new BattleReport(0, 0, 0, 0, 0);

    var result = new TurnEngine().Advance(state, report);

    Assert.NotEmpty(result.Factories, "Factories should remain populated.");
}

static void NormalizeUpgradesWarStateSchema()
{
    var state = WarState.CreateDefault() with { SchemaVersion = 0 };

    var normalized = state.Normalize();

    Assert.Equal(WarState.CurrentSchemaVersion, normalized.SchemaVersion);
}

static void NormalizeFillsCampaignMetadata()
{
    var state = WarState.CreateDefault() with
    {
        CampaignId = "",
        CampaignName = "",
        Theater = "",
        CreatedUtc = default,
        UpdatedUtc = default
    };

    var normalized = state.Normalize();

    Assert.True(!string.IsNullOrWhiteSpace(normalized.CampaignId), "Expected campaign id.");
    Assert.Equal("Russia vs Georgia/NATO", normalized.CampaignName);
    Assert.Equal("Caucasus", normalized.Theater);
    Assert.True(normalized.CreatedUtc != default, "Expected created timestamp.");
    Assert.True(normalized.UpdatedUtc != default, "Expected updated timestamp.");
}

static void TurnEnginePreservesCampaignMetadata()
{
    var created = DateTimeOffset.UtcNow.AddDays(-2);
    var state = WarState.CreateDefault() with
    {
        CampaignId = "campaign-test",
        CampaignName = "Test Campaign",
        CreatedUtc = created
    };

    var result = new TurnEngine().Advance(state, BattleReport.Empty);

    Assert.Equal("campaign-test", result.CampaignId);
    Assert.Equal("Test Campaign", result.CampaignName);
    Assert.Equal(created, result.CreatedUtc);
    Assert.True(result.UpdatedUtc > created, "Expected updated timestamp to advance.");
}

static void StateStoreCreatesBackupBeforeOverwrite()
{
    var root = CreateTempRoot();
    try
    {
        var store = new StateStore(new TestEnvironment(root));
        store.SaveAsync(WarState.CreateDefault()).GetAwaiter().GetResult();
        store.SaveAsync(WarState.CreateDefault() with { Turn = 2 }).GetAwaiter().GetResult();

        var backups = Directory.GetFiles(Path.Combine(root, "Data", "Backups"), "*.json");
        Assert.True(backups.Length == 1, "Expected one state backup before overwrite.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void StateStoreRecoversCorruptSave()
{
    var root = CreateTempRoot();
    try
    {
        var dataPath = Path.Combine(root, "Data");
        Directory.CreateDirectory(dataPath);
        File.WriteAllText(Path.Combine(dataPath, "war-state.json"), "{ broken json");

        var store = new StateStore(new TestEnvironment(root));
        var state = store.LoadAsync().GetAwaiter().GetResult();

        Assert.Equal(1, state.Turn);
        Assert.Equal(WarState.CurrentSchemaVersion, state.SchemaVersion);
        Assert.True(Directory.GetFiles(Path.Combine(dataPath, "Backups"), "*.json").Length == 1, "Expected corrupt save backup.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void StateStorePrunesOldBackups()
{
    var root = CreateTempRoot();
    try
    {
        var store = new StateStore(new TestEnvironment(root));
        store.SaveAsync(WarState.CreateDefault()).GetAwaiter().GetResult();
        for (var i = 0; i < 35; i++)
        {
            store.SaveAsync(WarState.CreateDefault() with { Turn = i + 2 }).GetAwaiter().GetResult();
        }

        var backups = Directory.GetFiles(Path.Combine(root, "Data", "Backups"), "*.json");
        Assert.Equal(30, backups.Length);
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void TurnEngineAppendsBattleReportHistory()
{
    var state = WarState.CreateDefault();
    var report = new BattleReport(25, 5, 2, 8, 10);

    var result = new TurnEngine().Advance(state, report);

    Assert.Equal(1, result.TurnHistory.Count);
    Assert.Equal(state.Turn, result.TurnHistory[0].Turn);
    Assert.Equal(report, result.TurnHistory[0].BattleReport);
    Assert.Equal("Blue momentum", result.TurnHistory[0].Summary);
}

static void TurnHistoryKeepsLatestTwentyEntries()
{
    var state = WarState.CreateDefault();
    var engine = new TurnEngine();

    for (var i = 0; i < 25; i++)
    {
        state = engine.Advance(state, BattleReport.Empty);
    }

    Assert.Equal(20, state.TurnHistory.Count);
    Assert.Equal(6, state.TurnHistory[0].Turn);
    Assert.Equal(25, state.TurnHistory[^1].Turn);
}

static void MissionPlanExporterWritesCampaignPlan()
{
    var root = CreateTempRoot();
    try
    {
        var templatePath = Path.Combine(root, "Data", "Templates");
        Directory.CreateDirectory(templatePath);
        CreateMinimalMiz(Path.Combine(templatePath, "template-test.miz"));
        var exporter = new MissionPlanExporter(new TestEnvironment(root));
        var state = WarState.CreateDefault() with
        {
            CampaignId = "test-campaign",
            CampaignName = "Test Campaign"
        };

        var result = exporter.ExportAsync(state).GetAwaiter().GetResult();

        Assert.True(File.Exists(result.FilePath), "Expected exported mission plan file.");
        Assert.Equal(state.Turn, result.Turn);
        Assert.Equal(state.MissionPackages.Count, result.PackageCount);
        Assert.Equal(state.GroundUnits.Count, result.GroundGroupCount);
        Assert.Equal(state.SupplyDepots.Count + state.Factories.Count, result.TargetCount);

        var json = File.ReadAllText(result.FilePath);
        Assert.True(json.Contains("_CLIENT_"), "Expected player slots policy in export.");
        Assert.True(json.Contains("frontlineMarkers"), "Expected frontline marker plans.");
        Assert.True(json.Contains("flightGroups"), "Expected flight group plans.");
        Assert.True(json.Contains("templateBindings"), "Expected template anchor bindings.");
        Assert.True(json.Contains("WL_OBJ_KUTAISI_BLUE"), "Expected objective anchor binding.");
        Assert.True(json.Contains("WL_OBJ_KRASNODAR_BLUE"), "Expected Krasnodar Center alias binding.");
        Assert.True(json.Contains("WL_AIRBASE_KUTAISI"), "Expected airbase anchor binding.");
        Assert.True(json.Contains("WL_HELI_BASE_KUTAISI_BLUE"), "Expected heli base anchor binding.");
        Assert.True(json.Contains("missingAirbaseAnchors"), "Expected missing airbase anchor report.");
        Assert.True(json.Contains("WL_FARP_KUTAISI_BLUE"), "Expected missing FARP anchor name.");
        Assert.True(json.Contains("WL_FRONT_01"), "Expected front anchor binding.");
        Assert.True(json.Contains("target-kutaisi-depot"), "Expected stable target ids.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void MissionPlanRoutesPackagesThroughAnchors()
{
    var root = CreateTempRoot();
    try
    {
        var templatePath = Path.Combine(root, "Data", "Templates");
        Directory.CreateDirectory(templatePath);
        CreateMinimalMiz(Path.Combine(templatePath, "template-test.miz"));
        var exporter = new MissionPlanExporter(new TestEnvironment(root));
        var state = WarState.CreateDefault() with
        {
            CampaignId = "test-campaign",
            MissionPackages =
            [
                new("BLUE-TEST", "blue", "Strike", "Kutaisi", "Planning", 2, "Georgian/NATO 11th Fighter Squadron")
            ]
        };

        var result = exporter.ExportAsync(state).GetAwaiter().GetResult();
        var json = File.ReadAllText(result.FilePath);
        var plan = JsonSerializer.Deserialize<MissionPlan>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Expected mission plan.");
        var flight = plan.FlightGroups.Single();

        Assert.Equal("WL_AIRBASE_KUTAISI", flight.DepartureAnchor);
        Assert.Equal("WL_OBJ_KUTAISI_BLUE", flight.TargetAnchor);
        Assert.Equal(3, flight.Route.Count);
        Assert.True(flight.Route.Any(waypoint => waypoint.Role == "frontline" && waypoint.Name == "WL_FRONT_01"), "Expected package route through front anchor.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void MissionPlanExporterPreparesMissionCopy()
{
    var root = CreateTempRoot();
    try
    {
        var templatePath = Path.Combine(root, "Data", "Templates");
        Directory.CreateDirectory(templatePath);
        CreateMinimalMiz(Path.Combine(templatePath, "template-test.miz"));
        var exporter = new MissionPlanExporter(new TestEnvironment(root));
        var state = WarState.CreateDefault() with
        {
            CampaignId = "test-campaign",
            MissionPackages =
            [
                new("BLUE-TEST", "blue", "Strike", "Kutaisi", "Planning", 2, "Georgian/NATO 11th Fighter Squadron")
            ]
        };

        var result = exporter.PrepareMissionAsync(state).GetAwaiter().GetResult();

        Assert.True(File.Exists(result.MizFilePath), "Expected prepared .miz copy.");
        Assert.True(File.Exists(result.MissionPlanFilePath), "Expected mission plan sidecar.");
        Assert.Equal("template-test.miz", result.TemplateFileName);
        Assert.Equal(state.Turn, result.Turn);
        using var archive = ZipFile.OpenRead(result.MizFilePath);
        Assert.True(archive.GetEntry("war-launcher/mission-plan.json") is not null, "Expected embedded mission plan.");
        var missionEntry = archive.GetEntry("mission") ?? throw new InvalidOperationException("Expected mission entry.");
        using var missionStream = missionEntry.Open();
        using var reader = new StreamReader(missionStream);
        var missionText = reader.ReadToEnd();
        Assert.True(missionText.Contains("DCS Persistent War Launcher", StringComparison.Ordinal), "Expected generated briefing.");
        Assert.True(missionText.Contains("Player slots are preserved", StringComparison.Ordinal), "Expected player slot briefing note.");
        Assert.True(missionText.Contains("WL_AI_flight-blue-test", StringComparison.Ordinal), "Expected generated AI package group.");
        Assert.True(missionText.Contains("[\"type\"] = \"F-16C_50\"", StringComparison.Ordinal), "Expected generated AI aircraft type.");
        Assert.True(missionText.Contains("[\"name\"] = \"WL_FRONT_01\"", StringComparison.Ordinal), "Expected generated route through front anchor.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void MissionTemplateInspectorReportsMissingTemplate()
{
    var root = CreateTempRoot();
    try
    {
        var inspector = new MissionTemplateInspector(new TestEnvironment(root));
        var result = inspector.InspectLatest();

        Assert.True(!result.IsReadable, "Expected missing template to be unreadable.");
        Assert.Equal(0, result.ClientSlotCount);
        Assert.True(result.Warnings.Any(warning => warning.Contains("No .miz template", StringComparison.Ordinal)), "Expected missing template warning.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void MissionTemplateInspectorReportsWlAnchors()
{
    var root = CreateTempRoot();
    try
    {
        var templatePath = Path.Combine(root, "Data", "Templates");
        Directory.CreateDirectory(templatePath);
        CreateMinimalMiz(Path.Combine(templatePath, "template-test.miz"));
        var inspector = new MissionTemplateInspector(new TestEnvironment(root));

        var result = inspector.InspectLatest();

        Assert.True(result.Anchors.Any(anchor => anchor.Name == "WL_OBJ_KUTAISI_BLUE"), "Expected WL objective anchor.");
        var anchor = result.Anchors.First(candidate => candidate.Name == "WL_OBJ_KUTAISI_BLUE");
        Assert.Equal("trigger-zone", anchor.Kind);
        Assert.Equal(100, (int)(anchor.X ?? 0));
        Assert.Equal(200, (int)(anchor.Y ?? 0));
        Assert.Equal(500, (int)(anchor.Radius ?? 0));
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static string CreateTempRoot()
{
    var root = Path.Combine(Path.GetTempPath(), "DcsWarLauncherTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    return root;
}

static void CreateMinimalMiz(string path)
{
    using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
    AddZipEntry(archive, "mission", "mission = {\n\t[\"trig\"] = {\n\t\t[\"actions\"] = {},\n\t\t[\"events\"] = {},\n\t\t[\"custom\"] = {},\n\t\t[\"func\"] = {},\n\t\t[\"flag\"] = {},\n\t\t[\"conditions\"] = {},\n\t\t[\"customStartup\"] = {},\n\t\t[\"funcStartup\"] = {},\n\t},\n\t[\"triggers\"] = {\n\t\t[\"zones\"] = {\n\t\t\t[1] = {\n\t\t\t\t[\"y\"] = 200,\n\t\t\t\t[\"x\"] = 100,\n\t\t\t\t[\"name\"] = \"WL_OBJ_KUTAISI_BLUE\",\n\t\t\t\t[\"radius\"] = 500,\n\t\t\t},\n\t\t\t[2] = {\n\t\t\t\t[\"y\"] = 300,\n\t\t\t\t[\"x\"] = 150,\n\t\t\t\t[\"name\"] = \"WL_FRONT_01\",\n\t\t\t\t[\"radius\"] = 700,\n\t\t\t},\n\t\t\t[3] = {\n\t\t\t\t[\"y\"] = 400,\n\t\t\t\t[\"x\"] = 200,\n\t\t\t\t[\"name\"] = \"WL_OBJ_KRASNODAR_BLUE\",\n\t\t\t\t[\"radius\"] = 500,\n\t\t\t},\n\t\t\t[4] = {\n\t\t\t\t[\"y\"] = 500,\n\t\t\t\t[\"x\"] = 250,\n\t\t\t\t[\"name\"] = \"WL_AIRBASE_KUTAISI\",\n\t\t\t\t[\"radius\"] = 1000,\n\t\t\t},\n\t\t\t[5] = {\n\t\t\t\t[\"y\"] = 550,\n\t\t\t\t[\"x\"] = 300,\n\t\t\t\t[\"name\"] = \"WL_HELI_BASE_KUTAISI_BLUE\",\n\t\t\t\t[\"radius\"] = 800,\n\t\t\t},\n\t\t},\n\t},\n\t[\"coalition\"] = {\n\t\t[\"blue\"] = {\n\t\t\t[\"name\"] = \"blue\",\n\t\t\t[\"country\"] = {\n\t\t\t\t[1] = {\n\t\t\t\t\t[\"name\"] = \"USA\",\n\t\t\t\t\t[\"id\"] = 2,\n\t\t\t\t},\n\t\t\t},\n\t\t},\n\t\t[\"red\"] = {\n\t\t\t[\"name\"] = \"red\",\n\t\t\t[\"country\"] = {\n\t\t\t\t[1] = {\n\t\t\t\t\t[\"name\"] = \"Russia\",\n\t\t\t\t\t[\"id\"] = 0,\n\t\t\t\t},\n\t\t\t},\n\t\t},\n\t},\n\t[\"descriptionText\"] = \"Template briefing\",\n\t[\"trigrules\"] = {},\n}");
    AddZipEntry(archive, "warehouses", "warehouses = {}");
    AddZipEntry(archive, "options", "options = {}");
    AddZipEntry(archive, "theatre", "Caucasus");
}

static void AddZipEntry(ZipArchive archive, string name, string content)
{
    var entry = archive.CreateEntry(name);
    using var stream = entry.Open();
    using var writer = new StreamWriter(stream);
    writer.Write(content);
}

static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void NotEmpty<T>(IReadOnlyCollection<T> items, string message)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Empty<T>(IReadOnlyCollection<T> items, string message)
    {
        if (items.Count != 0)
        {
            throw new InvalidOperationException(message);
        }
    }
}

sealed class TestEnvironment(string contentRootPath) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "DcsWarLauncher.Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = contentRootPath;
    public string EnvironmentName { get; set; } = "Test";
    public string WebRootPath { get; set; } = contentRootPath;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}

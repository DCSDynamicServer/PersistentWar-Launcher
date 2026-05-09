using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Mission;

public sealed class MissionPlanExporter(IWebHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _exportPath = Path.Combine(environment.ContentRootPath, "Data", "Exports");
    private readonly string _templatePath = Path.Combine(environment.ContentRootPath, "Data", "Templates");
    private readonly string _generatedPath = Path.Combine(environment.ContentRootPath, "Data", "Generated");

    public async Task<MissionExportResult> ExportAsync(WarState state)
    {
        state = state.Normalize();
        Directory.CreateDirectory(_exportPath);

        var generatedUtc = DateTimeOffset.UtcNow;
        var plan = BuildPlan(state, generatedUtc, InspectLatestTemplateAnchors());
        var safeCampaignId = SanitizeFileName(state.CampaignId);
        var fileName = $"{safeCampaignId}-turn-{state.Turn:D4}-mission-plan.json";
        var filePath = Path.Combine(_exportPath, fileName);

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, plan, JsonOptions);

        return new MissionExportResult(
            fileName,
            filePath,
            state.Turn,
            generatedUtc,
            plan.FlightGroups.Count,
            plan.GroundGroups.Count,
            plan.SupplyTargets.Count + plan.FactoryTargets.Count);
    }

    public MissionPlan Preview(WarState state)
    {
        state = state.Normalize();
        return BuildPlan(state, DateTimeOffset.UtcNow, InspectLatestTemplateAnchors());
    }

    public GeneratedMissionStatus GetLatestGeneratedMission()
    {
        var file = Directory.Exists(_generatedPath)
            ? Directory.GetFiles(_generatedPath, "*.miz")
                .Select(path => new FileInfo(path))
                .OrderByDescending(candidate => candidate.LastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        return file is null
            ? new GeneratedMissionStatus(null, null, null, null, false)
            : new GeneratedMissionStatus(
                file.Name,
                file.FullName,
                file.Length,
                file.LastWriteTimeUtc,
                true);
    }

    public async Task<PreparedMissionResult> PrepareMissionAsync(WarState state)
    {
        state = state.Normalize();
        Directory.CreateDirectory(_generatedPath);

        var template = GetLatestTemplate();
        var safeCampaignId = SanitizeFileName(state.CampaignId);
        var mizFileName = $"{safeCampaignId}-turn-{state.Turn:D4}.miz";
        var mizFilePath = Path.Combine(_generatedPath, mizFileName);
        File.Copy(template.FullName, mizFilePath, overwrite: true);

        var plan = await ExportAsync(state);
        await EmbedMissionPlanAsync(mizFilePath, plan.FilePath);
        await PatchMissionBriefingAsync(mizFilePath, state);
        return new PreparedMissionResult(
            mizFileName,
            mizFilePath,
            plan.FileName,
            plan.FilePath,
            state.Turn,
            plan.ExportedUtc,
            template.Name);
    }

    private static MissionPlan BuildPlan(
        WarState state,
        DateTimeOffset generatedUtc,
        IReadOnlyCollection<TemplateAnchorInspection> anchors)
    {
        var frontlineMarkers = state.Frontlines
            .Select(segment => new FrontlineMarkerPlan(
                StableId("frontline", segment.Name),
                segment.Name,
                segment.StartX,
                segment.StartY,
                segment.EndX,
                segment.EndY,
                segment.Momentum))
            .ToList();

        var flightGroups = state.MissionPackages
            .Select(package => new FlightGroupPlan(
                StableId("flight", package.Id),
                package.Coalition,
                package.Task,
                package.Target,
                package.Squadron,
                package.AircraftCount,
                package.Status))
            .ToList();

        var groundGroups = state.GroundUnits
            .Select(unit => new GroundGroupPlan(
                StableId("ground", unit.Name),
                unit.Name,
                unit.Coalition,
                unit.Type,
                unit.Location,
                unit.Strength,
                unit.Supply,
                unit.Readiness,
                unit.Posture))
            .ToList();

        var supplyTargets = state.SupplyDepots
            .Select(depot => new CampaignTargetPlan(
                StableId("target", depot.Name),
                depot.Name,
                depot.Coalition,
                depot.Location,
                "supply-depot",
                depot.Status,
                depot.Stores))
            .ToList();

        var factoryTargets = state.Factories
            .Select(factory => new CampaignTargetPlan(
                StableId("target", factory.Name),
                factory.Name,
                factory.Coalition,
                factory.Location,
                $"factory-{factory.OutputType}",
                factory.Status,
                factory.Health))
            .ToList();

        return new MissionPlan(
            state.CampaignId,
            state.CampaignName,
            state.Theater,
            state.Turn,
            generatedUtc,
            new TemplatePolicy(
                "PatchTemplate",
                "_CLIENT_",
                "Client/player slot groups are preserved from the template mission."),
            BuildTemplateBindings(state, anchors),
            state.Airbases,
            state.Objectives,
            frontlineMarkers,
            flightGroups,
            groundGroups,
            supplyTargets,
            factoryTargets);
    }

    private static TemplateBindings BuildTemplateBindings(
        WarState state,
        IReadOnlyCollection<TemplateAnchorInspection> anchors)
    {
        var objectiveAnchors = new List<ObjectiveAnchorBinding>();
        var missingObjectiveAnchors = new List<MissingObjectiveAnchor>();
        foreach (var objective in state.Objectives)
        {
            foreach (var coalition in new[] { "blue", "red" })
            {
                var expectedAnchorNames = ObjectiveAnchorNames(objective.Name, coalition).ToList();
                var anchor = expectedAnchorNames
                    .Select(name => anchors.FirstOrDefault(candidate =>
                        candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                        candidate.X is not null &&
                        candidate.Y is not null &&
                        candidate.Radius is not null))
                    .FirstOrDefault(candidate => candidate is not null);

                if (anchor is null)
                {
                    missingObjectiveAnchors.Add(new MissingObjectiveAnchor(objective.Name, coalition, expectedAnchorNames));
                    continue;
                }

                objectiveAnchors.Add(new ObjectiveAnchorBinding(
                        objective.Name,
                        coalition,
                        anchor.Name,
                        anchor.X!.Value,
                        anchor.Y!.Value,
                        anchor.Radius!.Value));
            }
        }

        var frontAnchors = anchors
            .Where(anchor =>
                anchor.Name.StartsWith("WL_FRONT_", StringComparison.OrdinalIgnoreCase) &&
                anchor.X is not null &&
                anchor.Y is not null &&
                anchor.Radius is not null)
            .Select(anchor => new FrontAnchorBinding(
                anchor.Name,
                ParseAnchorSequence(anchor.Name),
                anchor.X!.Value,
                anchor.Y!.Value,
                anchor.Radius!.Value))
            .OrderBy(anchor => anchor.Sequence)
            .ThenBy(anchor => anchor.AnchorName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var airbaseAnchors = new List<AirbaseAnchorBinding>();
        var missingAirbaseAnchors = new List<MissingAirbaseAnchor>();
        foreach (var airbase in state.Airbases)
        {
            foreach (var expectedGroup in AirbaseAnchorNames(airbase.Name).GroupBy(expected => expected.Type))
            {
                var expectedNames = expectedGroup.Select(expected => expected.Name).ToList();
                var anchor = expectedNames
                    .Select(name => anchors.FirstOrDefault(candidate =>
                        candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                        candidate.X is not null &&
                        candidate.Y is not null &&
                        candidate.Radius is not null))
                    .FirstOrDefault(candidate => candidate is not null);

                if (anchor is null)
                {
                    missingAirbaseAnchors.Add(new MissingAirbaseAnchor(airbase.Name, expectedGroup.Key, expectedNames));
                    continue;
                }

                airbaseAnchors.Add(new AirbaseAnchorBinding(
                        airbase.Name,
                        expectedGroup.Key,
                        anchor.Name,
                        anchor.X!.Value,
                        anchor.Y!.Value,
                        anchor.Radius!.Value));
            }
        }

        airbaseAnchors = airbaseAnchors
            .OrderBy(anchor => anchor.Airbase, StringComparer.OrdinalIgnoreCase)
            .ThenBy(anchor => anchor.AnchorType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TemplateBindings(
            objectiveAnchors,
            airbaseAnchors,
            frontAnchors,
            missingObjectiveAnchors,
            missingAirbaseAnchors);
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(character => invalid.Contains(character) ? '-' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "campaign" : sanitized;
    }

    private static string StableId(string prefix, string value)
    {
        var normalized = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        normalized = normalized.Trim('-');
        return string.IsNullOrWhiteSpace(normalized)
            ? prefix
            : $"{prefix}-{normalized}";
    }

    private FileInfo GetLatestTemplate()
    {
        var template = GetLatestTemplateOrNull();

        return template ?? throw new FileNotFoundException("No .miz template found.", _templatePath);
    }

    private FileInfo? GetLatestTemplateOrNull() =>
        Directory.Exists(_templatePath)
            ? Directory.GetFiles(_templatePath, "*.miz")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault()
            : null;

    private IReadOnlyCollection<TemplateAnchorInspection> InspectLatestTemplateAnchors()
    {
        var template = GetLatestTemplateOrNull();
        return template is null
            ? []
            : new MissionTemplateInspector(environment).Inspect(template.FullName).Anchors;
    }

    private static string ToAnchorToken(string value) =>
        new(
            value.Trim()
                .ToUpperInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '_')
                .ToArray());

    private static IEnumerable<string> ObjectiveAnchorNames(string objectiveName, string coalition)
    {
        var coalitionToken = coalition.ToUpperInvariant();
        yield return $"WL_OBJ_{ToAnchorToken(objectiveName)}_{coalitionToken}";

        if (objectiveName.Equals("Krasnodar Center", StringComparison.OrdinalIgnoreCase))
        {
            yield return $"WL_OBJ_KRASNODAR_{coalitionToken}";
        }
    }

    private static IEnumerable<(string Type, string Name)> AirbaseAnchorNames(string airbaseName)
    {
        var token = ToAnchorToken(airbaseName);
        yield return ("airbase", $"WL_AIRBASE_{token}");
        yield return ("heli-blue", $"WL_HELI_BASE_{token}_BLUE");
        yield return ("heli-red", $"WL_HELI_BASE_{token}_RED");
        yield return ("farp-blue", $"WL_FARP_{token}_BLUE");
        yield return ("farp-red", $"WL_FARP_{token}_RED");

        if (airbaseName.Equals("Krasnodar Center", StringComparison.OrdinalIgnoreCase))
        {
            yield return ("airbase", "WL_AIRBASE_KRASNODAR");
            yield return ("heli-blue", "WL_HELI_BASE_KRASNODAR_BLUE");
            yield return ("heli-red", "WL_HELI_BASE_KRASNODAR_RED");
            yield return ("farp-blue", "WL_FARP_KRASNODAR_BLUE");
            yield return ("farp-red", "WL_FARP_KRASNODAR_RED");
        }
    }

    private static int ParseAnchorSequence(string value)
    {
        var digits = new string(value.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return int.TryParse(digits, out var sequence) ? sequence : int.MaxValue;
    }

    private static async Task EmbedMissionPlanAsync(string mizFilePath, string missionPlanFilePath)
    {
        using var archive = ZipFile.Open(mizFilePath, ZipArchiveMode.Update);
        archive.GetEntry("war-launcher/mission-plan.json")?.Delete();
        var entry = archive.CreateEntry("war-launcher/mission-plan.json", CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await using var planStream = File.OpenRead(missionPlanFilePath);
        await planStream.CopyToAsync(entryStream);
    }

    private static async Task PatchMissionBriefingAsync(string mizFilePath, WarState state)
    {
        using var archive = ZipFile.Open(mizFilePath, ZipArchiveMode.Update);
        var missionEntry = archive.GetEntry("mission");
        if (missionEntry is null)
        {
            return;
        }

        string missionText;
        await using (var stream = missionEntry.Open())
        using (var reader = new StreamReader(stream))
        {
            missionText = await reader.ReadToEndAsync();
        }

        missionEntry.Delete();
        var patchedMission = PatchDescriptionText(missionText, BuildBriefingText(state));
        var newEntry = archive.CreateEntry("mission", CompressionLevel.Optimal);
        await using var entryStream = newEntry.Open();
        await using var writer = new StreamWriter(entryStream);
        await writer.WriteAsync(patchedMission);
    }

    private static string PatchDescriptionText(string missionText, string briefingText)
    {
        var escaped = ToLuaString(briefingText);
        var replacement = $"[\"descriptionText\"] = {escaped}";
        var pattern = "\\[\\\"descriptionText\\\"\\]\\s*=\\s*(?:\\\"(?:\\\\.|[^\\\"])*\\\"|\\[\\[.*?\\]\\])";
        if (Regex.IsMatch(missionText, pattern, RegexOptions.Singleline))
        {
            return Regex.Replace(missionText, pattern, replacement, RegexOptions.Singleline);
        }

        var insertAt = missionText.LastIndexOf('}');
        return insertAt < 0
            ? missionText
            : missionText.Insert(insertAt, $"\n\t{replacement},\n");
    }

    private static string BuildBriefingText(WarState state)
    {
        var objectives = string.Join(", ", state.Objectives.Select(objective => $"{objective.Name}: {objective.Owner} {objective.Strength}%"));
        var packages = state.MissionPackages.Count == 0
            ? "No AI packages planned."
            : string.Join(", ", state.MissionPackages.Select(package => $"{package.Coalition.ToUpperInvariant()} {package.Task} {package.Target}"));

        return $"""
            DCS Persistent War Launcher
            Campaign: {state.CampaignName}
            Theater: {state.Theater}
            Turn: {state.Turn}

            Player slots are preserved from the template mission.

            Objectives:
            {objectives}

            Planned AI packages:
            {packages}
            """;
    }

    private static string ToLuaString(string value) =>
        "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\n", StringComparison.Ordinal) + "\"";
}

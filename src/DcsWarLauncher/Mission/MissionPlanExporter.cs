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
        var plan = BuildPlan(state, generatedUtc);
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

    private static MissionPlan BuildPlan(WarState state, DateTimeOffset generatedUtc)
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
            state.Airbases,
            state.Objectives,
            frontlineMarkers,
            flightGroups,
            groundGroups,
            supplyTargets,
            factoryTargets);
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
        var template = Directory.Exists(_templatePath)
            ? Directory.GetFiles(_templatePath, "*.miz")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        return template ?? throw new FileNotFoundException("No .miz template found.", _templatePath);
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

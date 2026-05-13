using System.Text.Json;
using DcsWarLauncher.Domain;
using DcsWarLauncher.Infrastructure;

namespace DcsWarLauncher.Mission;

public sealed class MissionResultImporter(IWebHostEnvironment environment, IConfiguration? configuration = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _resultsPath = GetResultsPath(environment, configuration);

    public MissionResultStatus GetLatestResultStatus()
    {
        var file = GetLatestResultFile();
        return file is null
            ? new MissionResultStatus(null, null, null, null, false)
            : new MissionResultStatus(file.Name, file.FullName, file.Length, file.LastWriteTimeUtc, true);
    }

    public async Task<BattleReportImportResult> ImportLatestAsync()
    {
        var file = GetLatestResultFile()
            ?? throw new FileNotFoundException("No mission result file found.", _resultsPath);

        var report = await ImportAsync(file.FullName);
        return new BattleReportImportResult(file.Name, file.FullName, file.LastWriteTimeUtc, report);
    }

    public async Task<BattleReport> ImportAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return BattleReport.Empty;
        }

        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith("{", StringComparison.Ordinal) &&
            !trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            return ImportTextLines(content);
        }

        using var document = JsonDocument.Parse(content, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            return Clamp(ReadEvents(root.EnumerateArray()));
        }

        var reportRoot = TryGetProperty(root, "battleReport", "report", "result") ?? root;
        var direct = ReadDirectReport(reportRoot);
        var events = ReadEvents(root);

        return Clamp(new BattleReport(
            direct.BlueMissionSuccess + events.BlueMissionSuccess,
            direct.RedMissionSuccess + events.RedMissionSuccess,
            direct.BlueLosses + events.BlueLosses,
            direct.RedLosses + events.RedLosses,
            direct.AirSuperiority + events.AirSuperiority));
    }

    private static BattleReport ImportTextLines(string content)
    {
        var events = new List<JsonDocument>();
        try
        {
            var dcsAccumulator = new BattleReportAccumulator();
            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                if (trimmed.StartsWith("{", StringComparison.Ordinal))
                {
                    events.Add(JsonDocument.Parse(trimmed, new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip
                    }));
                    continue;
                }

                AddDcsLogEvent(dcsAccumulator, trimmed);
            }

            var jsonReport = ReadEvents(events.Select(document => document.RootElement));
            var dcsReport = dcsAccumulator.ToReport();
            return Clamp(new BattleReport(
                jsonReport.BlueMissionSuccess + dcsReport.BlueMissionSuccess,
                jsonReport.RedMissionSuccess + dcsReport.RedMissionSuccess,
                jsonReport.BlueLosses + dcsReport.BlueLosses,
                jsonReport.RedLosses + dcsReport.RedLosses,
                jsonReport.AirSuperiority + dcsReport.AirSuperiority));
        }
        finally
        {
            foreach (var document in events)
            {
                document.Dispose();
            }
        }
    }

    private static void AddDcsLogEvent(BattleReportAccumulator accumulator, string line)
    {
        const string marker = "event:";
        var markerIndex = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return;
        }

        var fields = ParseDcsEventFields(line[(markerIndex + marker.Length)..]);
        fields.TryGetValue("type", out var type);
        fields.TryGetValue("initiator_coalition", out var initiatorCoalition);
        fields.TryGetValue("target_coalition", out var targetCoalition);
        fields.TryGetValue("amount", out var amount);

        var coalition = NormalizeCoalition(initiatorCoalition ?? "");
        var target = NormalizeCoalition(targetCoalition ?? "");
        var value = int.TryParse(amount, out var parsedAmount) ? Math.Abs(parsedAmount) : 0;

        if (ContainsAny(type ?? "", "kill", "dead", "crash", "eject", "pilot dead"))
        {
            var lostCoalition = !string.IsNullOrWhiteSpace(target) ? target : coalition;
            if (lostCoalition == "blue")
            {
                accumulator.BlueLosses += 1;
            }
            else if (lostCoalition == "red")
            {
                accumulator.RedLosses += 1;
            }
        }

        if ((type ?? "").Equals("score", StringComparison.OrdinalIgnoreCase))
        {
            if (coalition == "blue")
            {
                accumulator.BlueMissionSuccess += value >= 50 ? 5 : 1;
                accumulator.AirSuperiority += value >= 50 ? 2 : 0;
            }
            else if (coalition == "red")
            {
                accumulator.RedMissionSuccess += value >= 50 ? 5 : 1;
                accumulator.AirSuperiority -= value >= 50 ? 2 : 0;
            }
        }
    }

    private static Dictionary<string, string> ParseDcsEventFields(string eventText)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawPart in eventText.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = rawPart.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = rawPart[..separator].Trim();
            var value = rawPart[(separator + 1)..].Trim();
            fields[key] = value;
        }

        return fields;
    }

    private FileInfo? GetLatestResultFile()
    {
        if (!Directory.Exists(_resultsPath))
        {
            return null;
        }

        return Directory.GetFiles(_resultsPath, "*.*")
            .Where(path =>
                path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static BattleReport ReadDirectReport(JsonElement element) =>
        new(
            ReadInt(element, "blueMissionSuccess", "blueSuccess", "blueScore"),
            ReadInt(element, "redMissionSuccess", "redSuccess", "redScore"),
            ReadInt(element, "blueLosses", "blueAircraftLosses", "blueUnitsLost"),
            ReadInt(element, "redLosses", "redAircraftLosses", "redUnitsLost"),
            ReadInt(element, "airSuperiority", "airPower", "airScore"));

    private static BattleReport ReadEvents(JsonElement root)
    {
        var eventsElement = TryGetProperty(root, "events", "missionEvents", "debriefing");
        if (eventsElement is null || eventsElement.Value.ValueKind != JsonValueKind.Array)
        {
            return BattleReport.Empty;
        }

        return ReadEvents(eventsElement.Value.EnumerateArray());
    }

    private static BattleReport ReadEvents(IEnumerable<JsonElement> missionEvents)
    {
        var accumulator = new BattleReportAccumulator();
        foreach (var missionEvent in missionEvents)
        {
            AddEvent(accumulator, missionEvent);
        }

        return accumulator.ToReport();
    }

    private static void AddEvent(BattleReportAccumulator accumulator, JsonElement missionEvent)
    {
        var type = ReadString(missionEvent, "type", "event", "eventType", "name");
        var coalition = NormalizeCoalition(ReadString(missionEvent, "coalition", "initiatorCoalition", "side", "winner"));
        var targetCoalition = NormalizeCoalition(ReadString(missionEvent, "targetCoalition", "victimCoalition", "lostCoalition"));
        var value = ReadInt(missionEvent, "value", "score", "points");

        if (IsLossEvent(type))
        {
            if (targetCoalition == "blue")
            {
                accumulator.BlueLosses += Math.Max(1, value);
            }
            else if (targetCoalition == "red")
            {
                accumulator.RedLosses += Math.Max(1, value);
            }
        }

        if (IsSuccessEvent(type))
        {
            if (coalition == "blue")
            {
                accumulator.BlueMissionSuccess += value == 0 ? 5 : value;
            }
            else if (coalition == "red")
            {
                accumulator.RedMissionSuccess += value == 0 ? 5 : value;
            }
        }

        if (IsObjectiveEvent(type))
        {
            if (coalition == "blue")
            {
                accumulator.BlueMissionSuccess += value == 0 ? 10 : value;
            }
            else if (coalition == "red")
            {
                accumulator.RedMissionSuccess += value == 0 ? 10 : value;
            }
        }

        if (IsAirSuperiorityEvent(type))
        {
            if (coalition == "blue")
            {
                accumulator.AirSuperiority += value == 0 ? 5 : value;
            }
            else if (coalition == "red")
            {
                accumulator.AirSuperiority -= value == 0 ? 5 : value;
            }
        }
    }

    private static bool IsLossEvent(string type) =>
        ContainsAny(type, "kill", "dead", "loss", "crash", "eject");

    private static bool IsSuccessEvent(string type) =>
        ContainsAny(type, "package-success", "mission-success", "task-complete");

    private static bool IsObjectiveEvent(string type) =>
        ContainsAny(type, "objective", "capture", "destroyed-target");

    private static bool IsAirSuperiorityEvent(string type) =>
        ContainsAny(type, "air-superiority", "air-superiority-shift", "airpower");

    private static bool ContainsAny(string value, params string[] needles) =>
        needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static BattleReport Clamp(BattleReport report) =>
        new(
            Math.Clamp(report.BlueMissionSuccess, -25, 25),
            Math.Clamp(report.RedMissionSuccess, -25, 25),
            Math.Clamp(report.BlueLosses, 0, 50),
            Math.Clamp(report.RedLosses, 0, 50),
            Math.Clamp(report.AirSuperiority, -25, 25));

    private static int ReadInt(JsonElement element, params string[] names)
    {
        var property = TryGetProperty(element, names);
        if (property is null)
        {
            return 0;
        }

        return property.Value.ValueKind switch
        {
            JsonValueKind.Number when property.Value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(property.Value.GetString(), out var number) => number,
            _ => 0
        };
    }

    private static string ReadString(JsonElement element, params string[] names)
    {
        var property = TryGetProperty(element, names);
        if (property is null || property.Value.ValueKind != JsonValueKind.String)
        {
            return "";
        }

        return property.Value.GetString() ?? "";
    }

    private static JsonElement? TryGetProperty(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (names.Any(name => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static string NormalizeCoalition(string value)
    {
        if (value.Equals("blue", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("allies", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("2", StringComparison.OrdinalIgnoreCase))
        {
            return "blue";
        }

        if (value.Equals("red", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("axis", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return "red";
        }

        return "";
    }

    private sealed class BattleReportAccumulator
    {
        public int BlueMissionSuccess { get; set; }
        public int RedMissionSuccess { get; set; }
        public int BlueLosses { get; set; }
        public int RedLosses { get; set; }
        public int AirSuperiority { get; set; }

        public BattleReport ToReport() => new(
            BlueMissionSuccess,
            RedMissionSuccess,
            BlueLosses,
            RedLosses,
            AirSuperiority);
    }

    private static string GetDataRoot(IWebHostEnvironment environment, IConfiguration? configuration) =>
        configuration is null
            ? DataPathResolver.GetDataRoot(environment)
            : DataPathResolver.GetDataRoot(environment, configuration);

    private static string GetResultsPath(IWebHostEnvironment environment, IConfiguration? configuration)
    {
        var configuredPath = configuration?["Launcher:MissionResultDirectory"];
        return string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(GetDataRoot(environment, configuration), "Results")
            : configuredPath;
    }
}

public sealed record MissionResultStatus(
    string? FileName,
    string? FilePath,
    long? SizeBytes,
    DateTimeOffset? LastModifiedUtc,
    bool Exists);

public sealed record BattleReportImportResult(
    string FileName,
    string FilePath,
    DateTimeOffset ImportedUtc,
    BattleReport BattleReport);

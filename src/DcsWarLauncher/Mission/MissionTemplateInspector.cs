using System.IO.Compression;
using System.Text.RegularExpressions;
using DcsWarLauncher.Infrastructure;

namespace DcsWarLauncher.Mission;

public sealed partial class MissionTemplateInspector(IWebHostEnvironment environment)
{
    private static readonly string[] RequiredFiles = ["mission", "warehouses", "options", "theatre"];
    private readonly string _templatePath = Path.Combine(DataPathResolver.GetDataRoot(environment), "Templates");

    public MissionTemplateInspection InspectLatest()
    {
        var directoryExists = Directory.Exists(_templatePath);
        var directoryFiles = directoryExists
            ? GetDirectoryFileNames(_templatePath)
            : [];
        var template = Directory.Exists(_templatePath)
            ? Directory.GetFiles(_templatePath, "*.miz")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        return template is null
            ? new MissionTemplateInspection(
                "",
                _templatePath,
                false,
                "",
                RequiredFiles,
                [],
                [],
                BuildMissingTemplateWarnings(directoryExists, directoryFiles),
                directoryExists,
                directoryFiles)
            : Inspect(template.FullName, directoryExists, directoryFiles);
    }

    public MissionTemplateInspection Inspect(string path) =>
        Inspect(
            path,
            Directory.Exists(Path.GetDirectoryName(path)),
            Directory.Exists(Path.GetDirectoryName(path))
                ? GetDirectoryFileNames(Path.GetDirectoryName(path)!)
                : []);

    private MissionTemplateInspection Inspect(
        string path,
        bool templateDirectoryExists,
        IReadOnlyCollection<string> templateDirectoryFiles)
    {
        var warnings = new List<string>();
        try
        {
            using var archive = ZipFile.OpenRead(path);
            var entries = archive.Entries
                .Select(entry => entry.FullName.Replace('\\', '/'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingFiles = RequiredFiles
                .Where(required => !entries.Contains(required))
                .ToList();
            var theater = ReadEntryText(archive, "theatre").Trim();
            var mission = ReadEntryText(archive, "mission");
            var clientGroups = InspectClientGroups(mission, warnings);
            var anchors = InspectAnchors(mission);

            if (clientGroups.Count == 0)
            {
                warnings.Add("No _CLIENT_ groups found.");
            }

            if (anchors.Count == 0)
            {
                warnings.Add("No WL_ template anchors found.");
            }

            return new MissionTemplateInspection(
                Path.GetFileName(path),
                path,
                missingFiles.Count == 0 && !string.IsNullOrWhiteSpace(mission),
                theater,
                missingFiles,
                clientGroups,
                anchors,
                warnings,
                templateDirectoryExists,
                templateDirectoryFiles);
        }
        catch (InvalidDataException)
        {
            return new MissionTemplateInspection(
                Path.GetFileName(path),
                path,
                false,
                "",
                RequiredFiles,
                [],
                [],
                ["Template is not a readable .miz/zip file."],
                templateDirectoryExists,
                templateDirectoryFiles);
        }
    }

    private static IReadOnlyCollection<string> BuildMissingTemplateWarnings(
        bool directoryExists,
        IReadOnlyCollection<string> directoryFiles)
    {
        if (!directoryExists)
        {
            return ["Template directory does not exist.", "No .miz template found."];
        }

        if (directoryFiles.Count == 0)
        {
            return ["Template directory is empty.", "No .miz template found."];
        }

        return
        [
            "No .miz template found.",
            $"Files seen in template directory: {string.Join(", ", directoryFiles.Take(8))}"
        ];
    }

    private static IReadOnlyCollection<string> GetDirectoryFileNames(string path) =>
        Directory.GetFiles(path)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<ClientGroupInspection> InspectClientGroups(string mission, List<string> warnings)
    {
        var groupMatches = GroupNameRegex().Matches(mission)
            .Where(match => match.Groups["name"].Value.Contains("_CLIENT_", StringComparison.OrdinalIgnoreCase))
            .Select(match => new
            {
                Name = match.Groups["name"].Value,
                Index = match.Index
            })
            .ToList();

        var groups = new List<ClientGroupInspection>();
        for (var index = 0; index < groupMatches.Count; index++)
        {
            var start = index == 0 ? 0 : groupMatches[index - 1].Index + groupMatches[index - 1].Name.Length;
            var length = groupMatches[index].Index - start;
            var block = mission.Substring(start, Math.Max(0, length));
            var skills = SkillRegex().Matches(block)
                .Select(match => match.Groups["skill"].Value)
                .ToList();
            var aircraft = TypeRegex().Matches(block)
                .Select(match => match.Groups["type"].Value)
                .Where(type => type is not "TakeOffParking" and not "Turning Point" and not "Refueling")
                .Distinct()
                .FirstOrDefault() ?? "unknown";
            var airdromeIds = AirdromeRegex().Matches(block)
                .Select(match => int.Parse(match.Groups["id"].Value))
                .Distinct()
                .Order()
                .ToList();
            var clientUnits = skills.Count(skill => skill.Equals("Client", StringComparison.OrdinalIgnoreCase));
            var aiUnits = skills.Count - clientUnits;

            if (aiUnits > 0)
            {
                warnings.Add($"{groupMatches[index].Name} contains {aiUnits} non-client unit(s).");
            }

            groups.Add(new ClientGroupInspection(
                groupMatches[index].Name,
                groupMatches[index].Name.StartsWith("BLUE_", StringComparison.OrdinalIgnoreCase) ? "blue" :
                    groupMatches[index].Name.StartsWith("RED_", StringComparison.OrdinalIgnoreCase) ? "red" : "unknown",
                aircraft,
                clientUnits,
                aiUnits,
                airdromeIds));
        }

        return groups;
    }

    private static List<TemplateAnchorInspection> InspectAnchors(string mission)
    {
        var anchorsByName = new Dictionary<string, TemplateAnchorInspection>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in WlNameRegex().Matches(mission))
        {
            var name = match.Groups["name"].Value;
            var zoneBlock = FindZoneBlock(mission, match.Index);
            if (zoneBlock is null)
            {
                anchorsByName.TryAdd(name, new TemplateAnchorInspection(name, "named-object", null, null, null));
                continue;
            }

            anchorsByName[name] = new TemplateAnchorInspection(
                name,
                "trigger-zone",
                ParseField(zoneBlock, "x"),
                ParseField(zoneBlock, "y"),
                ParseField(zoneBlock, "radius"));
        }

        return anchorsByName.Values
            .OrderBy(anchor => anchor.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? FindZoneBlock(string mission, int nameIndex)
    {
        var blockStart = mission.LastIndexOf("\n\t\t\t[", nameIndex, StringComparison.Ordinal);
        if (blockStart < 0)
        {
            return null;
        }

        var blockEnd = mission.IndexOf("\n\t\t\t}", nameIndex, StringComparison.Ordinal);
        return blockEnd < 0 || blockEnd <= blockStart
            ? null
            : mission[blockStart..blockEnd];
    }

    private static string ReadEntryText(ZipArchive archive, string name)
    {
        var entry = archive.Entries.FirstOrDefault(candidate =>
            candidate.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return "";
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [GeneratedRegex("\\[\\\"name\\\"\\]\\s*=\\s*\\\"(?<name>[^\\\"]+)\\\"")]
    private static partial Regex GroupNameRegex();

    [GeneratedRegex("\\[\\\"skill\\\"\\]\\s*=\\s*\\\"(?<skill>[^\\\"]+)\\\"")]
    private static partial Regex SkillRegex();

    [GeneratedRegex("\\[\\\"type\\\"\\]\\s*=\\s*\\\"(?<type>[^\\\"]+)\\\"")]
    private static partial Regex TypeRegex();

    [GeneratedRegex("\\[\\\"airdromeId\\\"\\]\\s*=\\s*(?<id>\\d+)")]
    private static partial Regex AirdromeRegex();

    [GeneratedRegex("\\[\\\"name\\\"\\]\\s*=\\s*\\\"(?<name>WL_[^\\\"]+)\\\"")]
    private static partial Regex WlNameRegex();

    private static double? ParseField(string block, string fieldName)
    {
        var match = Regex.Match(
            block,
            $"\\[\\\"{Regex.Escape(fieldName)}\\\"\\]\\s*=\\s*(?<value>-?\\d+(?:\\.\\d+)?)",
            RegexOptions.Singleline);
        return match.Success ? ParseDouble(match.Groups["value"].Value) : null;
    }

    private static double? ParseDouble(string value) =>
        double.TryParse(
            value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result)
            ? result
            : null;
}

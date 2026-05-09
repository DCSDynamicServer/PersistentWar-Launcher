using System.IO.Compression;
using System.Text.RegularExpressions;

namespace DcsWarLauncher.Mission;

public sealed partial class MissionTemplateInspector(IWebHostEnvironment environment)
{
    private static readonly string[] RequiredFiles = ["mission", "warehouses", "options", "theatre"];
    private readonly string _templatePath = Path.Combine(environment.ContentRootPath, "Data", "Templates");

    public MissionTemplateInspection InspectLatest()
    {
        var template = Directory.Exists(_templatePath)
            ? Directory.GetFiles(_templatePath, "*.miz")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        return template is null
            ? new MissionTemplateInspection("", _templatePath, false, "", RequiredFiles, [], ["No .miz template found."])
            : Inspect(template.FullName);
    }

    public MissionTemplateInspection Inspect(string path)
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

            if (clientGroups.Count == 0)
            {
                warnings.Add("No _CLIENT_ groups found.");
            }

            return new MissionTemplateInspection(
                Path.GetFileName(path),
                path,
                missingFiles.Count == 0 && !string.IsNullOrWhiteSpace(mission),
                theater,
                missingFiles,
                clientGroups,
                warnings);
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
                ["Template is not a readable .miz/zip file."]);
        }
    }

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
}

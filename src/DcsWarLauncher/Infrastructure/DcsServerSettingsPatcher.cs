using System.Text.RegularExpressions;

namespace DcsWarLauncher.Infrastructure;

public static class DcsServerSettingsPatcher
{
    public static DcsServerSettingsInspection Inspect(string serverSettingsText)
    {
        var root = Regex.IsMatch(serverSettingsText, @"\bcfg\s*=", RegexOptions.Singleline)
            ? "cfg"
            : Regex.IsMatch(serverSettingsText, @"\bsettings\s*=", RegexOptions.Singleline)
                ? "settings"
                : "";
        var hasListStartIndex = Regex.IsMatch(serverSettingsText, @"\[""listStartIndex""\]\s*=\s*\d+", RegexOptions.Singleline);
        var missionMatch = Regex.Match(
            serverSettingsText,
            @"\[""missionList""\]\s*=\s*\{.*?\[1\]\s*=\s*""(?<path>(?:\\.|[^""])*)""",
            RegexOptions.Singleline);
        var missionPath = missionMatch.Success
            ? missionMatch.Groups["path"].Value
                .Replace("\\\\", "\\", StringComparison.Ordinal)
                .Replace("\\\"", "\"", StringComparison.Ordinal)
            : null;

        return new DcsServerSettingsInspection(root, hasListStartIndex, missionPath);
    }

    public static string PatchMissionList(string serverSettingsText, string missionPath)
    {
        var inspection = Inspect(serverSettingsText);
        if (inspection.Root != "cfg")
        {
            throw new InvalidOperationException("serverSettings.lua must use DCS cfg root before missionList can be patched.");
        }

        var missionList = BuildMissionList(missionPath);
        var normalized = PatchListStartIndex(serverSettingsText);
        var pattern = @"\[""missionList""\]\s*=\s*\{.*?\}\s*,?(?:\s*-- end of \[""missionList""\])?";
        if (Regex.IsMatch(normalized, pattern, RegexOptions.Singleline))
        {
            return Regex.Replace(normalized, pattern, missionList, RegexOptions.Singleline);
        }

        var cfgMatch = Regex.Match(normalized, @"cfg\s*=\s*\{");
        if (cfgMatch.Success)
        {
            var insertAt = cfgMatch.Index + cfgMatch.Length;
            return normalized.Insert(insertAt, $"{Environment.NewLine}{missionList}");
        }

        throw new InvalidOperationException("serverSettings.lua does not contain a patchable DCS cfg table.");
    }

    private static string PatchListStartIndex(string serverSettingsText)
    {
        var pattern = @"\[""listStartIndex""\]\s*=\s*\d+\s*,?";
        if (Regex.IsMatch(serverSettingsText, pattern, RegexOptions.Singleline))
        {
            return Regex.Replace(serverSettingsText, pattern, "[\"listStartIndex\"] = 1,", RegexOptions.Singleline);
        }

        var cfgMatch = Regex.Match(serverSettingsText, @"cfg\s*=\s*\{");
        return cfgMatch.Success
            ? serverSettingsText.Insert(cfgMatch.Index + cfgMatch.Length, $"{Environment.NewLine}\t[\"listStartIndex\"] = 1,")
            : serverSettingsText;
    }

    private static string BuildMissionList(string missionPath)
    {
        var escapedMissionPath = missionPath
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return string.Join(Environment.NewLine,
            "\t[\"missionList\"] = ",
            "\t{",
            $"\t\t[1] = \"{escapedMissionPath}\",",
            "\t}, -- end of [\"missionList\"]");
    }
}

public sealed record DcsServerSettingsInspection(
    string Root,
    bool HasListStartIndex,
    string? MissionPath);

using System.Text.RegularExpressions;

namespace DcsWarLauncher.Infrastructure;

public static class DcsServerSettingsPatcher
{
    public static string PatchMissionList(string serverSettingsText, string missionPath)
    {
        var normalized = NormalizeRoot(serverSettingsText);
        var missionList = BuildMissionList(missionPath);
        normalized = PatchListStartIndex(normalized);
        var pattern = @"\[""missionList""\]\s*=\s*\{.*?\n\s*\},\s*-- end of \[""missionList""\]";
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

        return $"cfg = {{{Environment.NewLine}\t[\"listStartIndex\"] = 1,{Environment.NewLine}{missionList}{Environment.NewLine}}} -- end of cfg";
    }

    private static string NormalizeRoot(string serverSettingsText)
    {
        if (Regex.IsMatch(serverSettingsText, @"\bcfg\s*=", RegexOptions.Singleline))
        {
            return serverSettingsText;
        }

        return Regex.IsMatch(serverSettingsText, @"\bsettings\s*=", RegexOptions.Singleline)
            ? Regex.Replace(serverSettingsText, @"\bsettings\s*=", "cfg = ", RegexOptions.Singleline)
            : serverSettingsText;
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

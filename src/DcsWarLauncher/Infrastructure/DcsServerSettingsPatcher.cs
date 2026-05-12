using System.Text.RegularExpressions;

namespace DcsWarLauncher.Infrastructure;

public static class DcsServerSettingsPatcher
{
    public static string PatchMissionList(string serverSettingsText, string missionPath)
    {
        var missionList = BuildMissionList(missionPath);
        var pattern = @"\[""missionList""\]\s*=\s*\{.*?\n\s*\},\s*-- end of \[""missionList""\]";
        if (Regex.IsMatch(serverSettingsText, pattern, RegexOptions.Singleline))
        {
            return Regex.Replace(serverSettingsText, pattern, missionList, RegexOptions.Singleline);
        }

        var settingsMatch = Regex.Match(serverSettingsText, @"settings\s*=\s*\{");
        if (settingsMatch.Success)
        {
            var insertAt = settingsMatch.Index + settingsMatch.Length;
            return serverSettingsText.Insert(insertAt, $"{Environment.NewLine}{missionList}");
        }

        return $"settings = {{{Environment.NewLine}{missionList}{Environment.NewLine}}}";
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

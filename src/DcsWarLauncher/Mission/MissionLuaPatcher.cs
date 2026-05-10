using System.Globalization;
using System.Text;

namespace DcsWarLauncher.Mission;

internal static class MissionLuaPatcher
{
    private const int GeneratedGroupIdBase = 900000;
    private const int GeneratedUnitIdBase = 910000;

    public static string PatchGeneratedAiFlights(string missionText, MissionPlan plan)
    {
        var patched = missionText;
        var index = 0;
        foreach (var group in plan.FlightGroups.Where(group => group.Route.Count >= 2))
        {
            var category = IsHelicopter(group.Aircraft) ? "helicopter" : "plane";
            var country = group.Coalition.Equals("red", StringComparison.OrdinalIgnoreCase)
                ? new CoalitionCountry("Russia", 0)
                : new CoalitionCountry("USA", 2);
            var lua = BuildFlightGroupLua(group, category, ++index);
            patched = InsertGroupIntoCoalition(patched, group.Coalition, country, category, lua);
        }

        return patched;
    }

    private static string InsertGroupIntoCoalition(
        string missionText,
        string coalition,
        CoalitionCountry country,
        string category,
        string groupLua)
    {
        var coalitionTable = FindNamedTable(missionText, "coalition", 0);
        if (coalitionTable is null)
        {
            return missionText;
        }

        var sideTable = FindNamedTable(missionText, coalition.ToLowerInvariant(), coalitionTable.Value.OpenBrace);
        if (sideTable is null || sideTable.Value.CloseBrace > coalitionTable.Value.CloseBrace)
        {
            return missionText;
        }

        var countryTable = FindNamedTable(missionText, "country", sideTable.Value.OpenBrace);
        if (countryTable is null || countryTable.Value.CloseBrace > sideTable.Value.CloseBrace)
        {
            return missionText;
        }

        var countryBlock = FindCountryBlock(missionText, countryTable.Value, country);
        return countryBlock is null
            ? InsertGeneratedCountry(missionText, countryTable.Value, country, category, groupLua)
            : InsertGroupIntoCountry(missionText, countryBlock.Value, category, groupLua);
    }

    private static string InsertGeneratedCountry(
        string missionText,
        LuaTable countryTable,
        CoalitionCountry country,
        string category,
        string groupLua)
    {
        var nextCountryIndex = NextDirectArrayIndex(missionText, countryTable);
        var block = string.Join(Environment.NewLine,
            "",
            $"\t\t\t\t[{nextCountryIndex}] =",
            "\t\t\t\t{",
            $"\t\t\t\t\t[\"name\"] = \"{country.Name}\",",
            $"\t\t\t\t\t[\"id\"] = {country.Id},",
            $"\t\t\t\t\t[\"{category}\"] =",
            "\t\t\t\t\t{",
            "\t\t\t\t\t\t[\"group\"] =",
            "\t\t\t\t\t\t{",
            "\t\t\t\t\t\t\t[1] =",
            Indent(groupLua, 7),
            "\t\t\t\t\t\t}, -- end of [\"group\"]",
            $"\t\t\t\t\t}}, -- end of [\"{category}\"]",
            $"\t\t\t\t}}, -- end of [{nextCountryIndex}]");

        return missionText.Insert(countryTable.CloseBrace, block);
    }

    private static string InsertGroupIntoCountry(
        string missionText,
        LuaTable countryBlock,
        string category,
        string groupLua)
    {
        var categoryTable = FindNamedTable(missionText, category, countryBlock.OpenBrace);
        if (categoryTable is null || categoryTable.Value.CloseBrace > countryBlock.CloseBrace)
        {
            var block = string.Join(Environment.NewLine,
                "",
                $"\t\t\t\t\t[\"{category}\"] =",
                "\t\t\t\t\t{",
                "\t\t\t\t\t\t[\"group\"] =",
                "\t\t\t\t\t\t{",
                "\t\t\t\t\t\t\t[1] =",
                Indent(groupLua, 7),
                "\t\t\t\t\t\t}, -- end of [\"group\"]",
                $"\t\t\t\t\t}}, -- end of [\"{category}\"]");

            return missionText.Insert(countryBlock.CloseBrace, block);
        }

        var groupTable = FindNamedTable(missionText, "group", categoryTable.Value.OpenBrace);
        if (groupTable is null || groupTable.Value.CloseBrace > categoryTable.Value.CloseBrace)
        {
            var block = string.Join(Environment.NewLine,
                "",
                "\t\t\t\t\t\t[\"group\"] =",
                "\t\t\t\t\t\t{",
                "\t\t\t\t\t\t\t[1] =",
                Indent(groupLua, 7),
                "\t\t\t\t\t\t}, -- end of [\"group\"]");

            return missionText.Insert(categoryTable.Value.CloseBrace, block);
        }

        var nextGroupIndex = NextDirectArrayIndex(missionText, groupTable.Value);
        var groupBlock = string.Join(Environment.NewLine,
            "",
            $"\t\t\t\t\t\t\t[{nextGroupIndex}] =",
            Indent(groupLua, 7),
            "");

        return missionText.Insert(groupTable.Value.CloseBrace, groupBlock);
    }

    private static string BuildFlightGroupLua(FlightGroupPlan group, string category, int sequence)
    {
        var groupId = GeneratedGroupIdBase + sequence;
        var first = group.Route.First();
        var sb = new StringBuilder();
        sb.AppendLine("\t\t\t\t\t\t\t\t{");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"visible\"] = false,");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"lateActivation\"] = false,");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"modulation\"] = 0,");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"tasks\"] = {},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"radioSet\"] = false,");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t[\"task\"] = {LuaString(ToDcsTask(group.Task, category))},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"uncontrolled\"] = false,");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t[\"groupId\"] = {groupId},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t[\"name\"] = {LuaString($"WL_AI_{group.Id}")},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t[\"x\"] = {LuaNumber(first.X)},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t[\"y\"] = {LuaNumber(first.Y)},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"route\"] =");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t{");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t[\"points\"] =");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t{");
        var pointIndex = 0;
        foreach (var waypoint in group.Route)
        {
            sb.Append(BuildRoutePointLua(waypoint, ++pointIndex, category));
        }

        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t}, -- end of [\"points\"]");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t}, -- end of [\"route\"]");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"units\"] =");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t{");
        var aircraftType = ToDcsAircraftType(group.Aircraft);
        var unitCount = Math.Clamp(group.AircraftCount, 1, 4);
        for (var unitIndex = 1; unitIndex <= unitCount; unitIndex++)
        {
            sb.Append(BuildUnitLua(group, aircraftType, groupId, sequence, unitIndex, first, category));
        }

        sb.AppendLine("\t\t\t\t\t\t\t\t\t}, -- end of [\"units\"]");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"communication\"] = true,");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t[\"start_time\"] = 0,");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t[\"frequency\"] = {DefaultFrequency(group.Coalition)},");
        sb.AppendLine("\t\t\t\t\t\t\t\t}, -- end of generated AI group");
        return sb.ToString();
    }

    private static string BuildRoutePointLua(RouteWaypointPlan waypoint, int index, string category)
    {
        var altitude = category.Equals("helicopter", StringComparison.OrdinalIgnoreCase) ? 250 : 2500;
        var speed = category.Equals("helicopter", StringComparison.OrdinalIgnoreCase) ? 55 : 220;
        var eta = Math.Max(0, (index - 1) * 300);
        var sb = new StringBuilder();
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t[{index}] =");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t{");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[\"alt\"] = {altitude},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"action\"] = \"Turning Point\",");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"alt_type\"] = \"BARO\",");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[\"speed\"] = {speed},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"type\"] = \"Turning Point\",");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[\"ETA\"] = {eta},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"ETA_locked\"] = true,");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"speed_locked\"] = false,");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[\"x\"] = {LuaNumber(waypoint.X)},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[\"y\"] = {LuaNumber(waypoint.Y)},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[\"name\"] = {LuaString(waypoint.Name)},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"task\"] =");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t{");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t\t[\"id\"] = \"ComboTask\",");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t\t[\"params\"] =");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t\t{");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t\t\t[\"tasks\"] = {},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t\t},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t}}, -- end of [{index}]");
        return sb.ToString();
    }

    private static string BuildUnitLua(
        FlightGroupPlan group,
        string aircraftType,
        int groupId,
        int sequence,
        int unitIndex,
        RouteWaypointPlan first,
        string category)
    {
        var unitId = GeneratedUnitIdBase + sequence * 10 + unitIndex;
        var offset = (unitIndex - 1) * 35;
        var altitude = category.Equals("helicopter", StringComparison.OrdinalIgnoreCase) ? 250 : 2500;
        var speed = category.Equals("helicopter", StringComparison.OrdinalIgnoreCase) ? 55 : 220;
        var sb = new StringBuilder();
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t[{unitIndex}] =");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t{");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t[\"alt\"] = {altitude},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t[\"alt_type\"] = \"BARO\",");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t[\"skill\"] = \"High\",");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t[\"speed\"] = {speed},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t[\"type\"] = {LuaString(aircraftType)},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t[\"unitId\"] = {unitId},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t[\"psi\"] = 0,");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t[\"x\"] = {LuaNumber(first.X + offset)},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t[\"y\"] = {LuaNumber(first.Y + offset)},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t[\"name\"] = {LuaString($"WL_AI_{group.Id}_{unitIndex}")},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t[\"payload\"] =");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t{");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"pylons\"] = {},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[\"fuel\"] = {DefaultFuel(aircraftType)},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"flare\"] = 60,");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"chaff\"] = 60,");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[\"gun\"] = 100,");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t},");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t[\"heading\"] = 0,");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t[\"callsign\"] =");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t{");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t\t[1] = 1,");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[2] = {sequence},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[3] = {unitIndex},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t\t[\"name\"] = \"Enfield{sequence}{unitIndex}\",");
        sb.AppendLine("\t\t\t\t\t\t\t\t\t\t\t},");
        sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t}}, -- end of [{unitIndex}]");
        return sb.ToString();
    }

    private static LuaTable? FindNamedTable(string text, string name, int startIndex)
    {
        var pattern = $"[\"{name}\"]";
        var nameIndex = text.IndexOf(pattern, startIndex, StringComparison.OrdinalIgnoreCase);
        if (nameIndex < 0)
        {
            return null;
        }

        var openBrace = text.IndexOf('{', nameIndex);
        if (openBrace < 0)
        {
            return null;
        }

        var closeBrace = FindMatchingBrace(text, openBrace);
        return closeBrace < 0 ? null : new LuaTable(openBrace, closeBrace);
    }

    private static LuaTable? FindCountryBlock(string text, LuaTable countryTable, CoalitionCountry country)
    {
        var index = countryTable.OpenBrace + 1;
        while (index < countryTable.CloseBrace)
        {
            var entryStart = text.IndexOf('[', index);
            if (entryStart < 0 || entryStart >= countryTable.CloseBrace)
            {
                return null;
            }

            var openBrace = text.IndexOf('{', entryStart);
            if (openBrace < 0 || openBrace >= countryTable.CloseBrace)
            {
                return null;
            }

            var closeBrace = FindMatchingBrace(text, openBrace);
            if (closeBrace < 0 || closeBrace > countryTable.CloseBrace)
            {
                return null;
            }

            var block = text[openBrace..closeBrace];
            if (block.Contains($"[\"id\"] = {country.Id}", StringComparison.Ordinal) ||
                block.Contains($"[\"name\"] = \"{country.Name}\"", StringComparison.OrdinalIgnoreCase))
            {
                return new LuaTable(openBrace, closeBrace);
            }

            index = closeBrace + 1;
        }

        return null;
    }

    private static int FindMatchingBrace(string text, int openBrace)
    {
        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = openBrace; index < text.Length; index++)
        {
            var character = text[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (character == '"')
            {
                inString = true;
                continue;
            }

            if (character == '{')
            {
                depth++;
            }
            else if (character == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private static int NextDirectArrayIndex(string text, LuaTable table)
    {
        var max = 0;
        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = table.OpenBrace + 1; index < table.CloseBrace; index++)
        {
            var character = text[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (character == '"')
            {
                inString = true;
                continue;
            }

            if (character == '{')
            {
                depth++;
                continue;
            }

            if (character == '}')
            {
                depth--;
                continue;
            }

            if (depth == 0 && character == '[')
            {
                var end = text.IndexOf(']', index + 1);
                if (end > index && end < table.CloseBrace)
                {
                    var token = text[(index + 1)..end];
                    if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                    {
                        max = Math.Max(max, value);
                    }
                }
            }
        }

        return max + 1;
    }

    private static string Indent(string text, int tabs)
    {
        var prefix = new string('\t', tabs);
        return string.Join(
            Environment.NewLine,
            text.TrimEnd().Split(["\r\n", "\n"], StringSplitOptions.None).Select(line => prefix + line));
    }

    private static string ToDcsAircraftType(string aircraft) =>
        aircraft.ToUpperInvariant() switch
        {
            "F-16C" => "F-16C_50",
            "F/A-18C" => "FA-18C_hornet",
            "AH-64D" => "AH-64D_BLK_II",
            "OH-58D" => "OH58D",
            "KA-50" => "Ka-50",
            _ => aircraft
        };

    private static string ToDcsTask(string task, string category)
    {
        if (category.Equals("helicopter", StringComparison.OrdinalIgnoreCase))
        {
            return "CAS";
        }

        return task.ToUpperInvariant() switch
        {
            "CAP" or "BARCAP" or "INTERCEPT" => "CAP",
            "CAS" => "CAS",
            "STRIKE" or "OCA" => "Ground Attack",
            _ => "CAP"
        };
    }

    private static int DefaultFuel(string aircraftType) =>
        aircraftType switch
        {
            "F-16C_50" => 3249,
            "FA-18C_hornet" => 4900,
            "Su-27" => 9400,
            "Su-25T" => 3790,
            "Ka-50" => 1450,
            "AH-64D_BLK_II" => 1420,
            "OH58D" => 400,
            _ => 3000
        };

    private static int DefaultFrequency(string coalition) =>
        coalition.Equals("red", StringComparison.OrdinalIgnoreCase) ? 124 : 305;

    private static bool IsHelicopter(string aircraft)
    {
        var normalized = aircraft.Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        return normalized.StartsWith("AH", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("OH", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("KA", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("MI", StringComparison.OrdinalIgnoreCase);
    }

    private static string LuaString(string value) =>
        "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static string LuaNumber(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private readonly record struct LuaTable(int OpenBrace, int CloseBrace);

    private sealed record CoalitionCountry(string Name, int Id);
}

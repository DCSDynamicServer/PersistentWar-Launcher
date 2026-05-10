using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DcsWarLauncher.Mission;

public static class MissionWarehousePatcher
{
    public static string PatchWarehousePlan(string warehouseText, MissionPlan plan)
    {
        if (plan.WarehousePatches.Count == 0)
        {
            return warehouseText;
        }

        warehouseText = PatchKnownDcsAirportWarehouses(warehouseText, plan.WarehousePatches);

        var markerStart = "-- WL_WAREHOUSE_PATCH_BEGIN";
        var markerEnd = "-- WL_WAREHOUSE_PATCH_END";
        var patchBlock = BuildPatchBlock(plan.WarehousePatches, markerStart, markerEnd);
        var start = warehouseText.IndexOf(markerStart, StringComparison.Ordinal);
        var end = warehouseText.IndexOf(markerEnd, StringComparison.Ordinal);

        if (start >= 0 && end > start)
        {
            end += markerEnd.Length;
            return warehouseText[..start] + patchBlock + warehouseText[end..];
        }

        return warehouseText.TrimEnd() + Environment.NewLine + Environment.NewLine + patchBlock;
    }

    private static string BuildPatchBlock(
        IReadOnlyCollection<WarehousePatchPlan> patches,
        string markerStart,
        string markerEnd)
    {
        var builder = new StringBuilder();
        builder.AppendLine(markerStart);
        builder.AppendLine("warehouses = warehouses or {}");
        builder.AppendLine("warehouses[\"warLauncher\"] = {");
        builder.AppendLine("\t[\"version\"] = 1,");
        builder.AppendLine("\t[\"mode\"] = \"campaign-supply-shadow\",");
        builder.AppendLine("\t[\"airbases\"] = {");

        var index = 1;
        foreach (var patch in patches)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t[{index}] = {{");
            builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t\t[\"id\"] = {ToLuaString(patch.Id)},");
            builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t\t[\"airbase\"] = {ToLuaString(patch.Airbase)},");
            builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t\t[\"coalition\"] = {ToLuaString(patch.Coalition)},");
            if (patch.DcsWarehouseId is not null)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t\t[\"dcsWarehouseId\"] = {patch.DcsWarehouseId.Value},");
            }

            builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t\t[\"fuelPercent\"] = {patch.FuelPercent},");
            builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t\t[\"ammoPercent\"] = {patch.AmmoPercent},");
            builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t\t[\"aircraftPercent\"] = {patch.AircraftPercent},");
            builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t\t[\"status\"] = {ToLuaString(patch.Status)},");
            builder.AppendLine("\t\t},");
            index++;
        }

        builder.AppendLine("\t},");
        builder.AppendLine("}");
        builder.Append(markerEnd);
        return builder.ToString();
    }

    private static string PatchKnownDcsAirportWarehouses(
        string warehouseText,
        IReadOnlyCollection<WarehousePatchPlan> patches)
    {
        foreach (var patch in patches.Where(candidate => candidate.DcsWarehouseId is not null))
        {
            var id = patch.DcsWarehouseId!.Value;
            var pattern = $@"(?<head>\[\s*{id}\s*\]\s*=\s*\{{)(?<body>.*?)(?<tail>\n\s*\}},\s*-- end of \[\s*{id}\s*\])";
            warehouseText = Regex.Replace(
                warehouseText,
                pattern,
                match => match.Groups["head"].Value +
                    PatchAirportWarehouseBody(match.Groups["body"].Value, patch) +
                    match.Groups["tail"].Value,
                RegexOptions.Singleline);
        }

        return warehouseText;
    }

    private static string PatchAirportWarehouseBody(string body, WarehousePatchPlan patch)
    {
        body = ReplaceBoolean(body, "unlimitedFuel", false);
        body = ReplaceNumber(body, "OperatingLevel_Fuel", OperatingLevel(patch.FuelPercent));
        body = ReplaceNumber(body, "OperatingLevel_Eqp", OperatingLevel(patch.AmmoPercent));
        body = ReplaceNumber(body, "OperatingLevel_Air", OperatingLevel(patch.AircraftPercent));
        body = ReplaceFuelInit(body, "jet_fuel", patch.FuelPercent);
        body = ReplaceFuelInit(body, "gasoline", patch.FuelPercent);
        body = ReplaceFuelInit(body, "diesel", patch.FuelPercent);
        body = ReplaceFuelInit(body, "methanol_mixture", patch.FuelPercent);
        body = ReplaceString(body, "coalition", patch.Coalition.ToUpperInvariant());
        return body;
    }

    private static int OperatingLevel(int percent) => Math.Clamp((int)Math.Round(percent / 10d), 0, 10);

    private static string ReplaceBoolean(string text, string key, bool value) =>
        Regex.Replace(
            text,
            $@"(\[""{Regex.Escape(key)}""\]\s*=\s*)(true|false)",
            match => match.Groups[1].Value + value.ToString().ToLowerInvariant(),
            RegexOptions.Singleline);

    private static string ReplaceNumber(string text, string key, int value) =>
        Regex.Replace(
            text,
            $@"(\[""{Regex.Escape(key)}""\]\s*=\s*)-?\d+(?:\.\d+)?",
            match => match.Groups[1].Value + value.ToString(CultureInfo.InvariantCulture),
            RegexOptions.Singleline);

    private static string ReplaceString(string text, string key, string value) =>
        Regex.Replace(
            text,
            $@"(\[""{Regex.Escape(key)}""\]\s*=\s*)""[^""]*""",
            match => match.Groups[1].Value + ToLuaString(value),
            RegexOptions.Singleline);

    private static string ReplaceFuelInit(string text, string fuelKey, int value)
    {
        var pattern = $@"(?<head>\[""{Regex.Escape(fuelKey)}""\]\s*=\s*\{{(?<body>.*?))(?<tail>\n\s*\}},\s*-- end of \[""{Regex.Escape(fuelKey)}""\])";
        return Regex.Replace(
            text,
            pattern,
            match => match.Groups["head"].Value +
                ReplaceNumber(match.Groups["body"].Value, "InitFuel", value) +
                match.Groups["tail"].Value,
            RegexOptions.Singleline);
    }

    private static string ToLuaString(string value) =>
        "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\n", StringComparison.Ordinal) + "\"";
}

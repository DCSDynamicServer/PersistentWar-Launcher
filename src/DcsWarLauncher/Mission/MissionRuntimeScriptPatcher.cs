using System.Text;
using System.Text.RegularExpressions;
using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Mission;

public static class MissionRuntimeScriptPatcher
{
    public static string Patch(string missionText, WarState state)
    {
        var script = BuildScript(state);
        var escapedScript = ToLuaString(script);
        var startupEntry = $"\t\t\t[1] = {escapedScript},\n";

        var customStartupPattern = @"\[""customStartup""\]\s*=\s*\{.*?\}";
        if (Regex.IsMatch(missionText, customStartupPattern, RegexOptions.Singleline))
        {
            return Regex.Replace(
                missionText,
                customStartupPattern,
                $"[\"customStartup\"] = \n\t\t{{\n{startupEntry}\t\t}}",
                RegexOptions.Singleline);
        }

        var trigPattern = @"\[""trig""\]\s*=\s*\{";
        var match = Regex.Match(missionText, trigPattern);
        return match.Success
            ? missionText.Insert(match.Index + match.Length, $"\n\t\t[\"customStartup\"] = \n\t\t{{\n{startupEntry}\t\t}},")
            : missionText;
    }

    private static string BuildScript(WarState state)
    {
        var durationSeconds = Math.Max(1, state.TurnDurationHours) * 60 * 60;
        var fileName = $"{SanitizeFileName(state.CampaignId)}-turn-{state.Turn:D4}-result.json";
        return $$"""
            WL = WL or {}
            WL.turnStartedAt = timer.getTime()
            WL.turnDurationSeconds = {{durationSeconds}}
            WL.resultFileName = "{{fileName}}"
            WL.events = WL.events or {}
            WL.finalized = false

            local function wl_escape(value)
                value = tostring(value or "")
                value = value:gsub("\\", "\\\\")
                value = value:gsub("\"", "\\\"")
                value = value:gsub("\r", "\\r")
                value = value:gsub("\n", "\\n")
                return value
            end

            local function wl_coalition_name(value)
                if value == coalition.side.BLUE then
                    return "blue"
                end
                if value == coalition.side.RED then
                    return "red"
                end
                return ""
            end

            local function wl_event_type(event)
                if event.id == world.event.S_EVENT_KILL then
                    return "kill"
                end
                if event.id == world.event.S_EVENT_DEAD then
                    return "dead"
                end
                if event.id == world.event.S_EVENT_CRASH then
                    return "crash"
                end
                if event.id == world.event.S_EVENT_EJECTION then
                    return "eject"
                end
                if event.id == world.event.S_EVENT_TAKEOFF then
                    return "takeoff"
                end
                if event.id == world.event.S_EVENT_LAND then
                    return "land"
                end
                return nil
            end

            local function wl_unit_coalition(unit)
                if unit and unit.getCoalition then
                    local ok, side = pcall(function() return unit:getCoalition() end)
                    if ok then
                        return wl_coalition_name(side)
                    end
                end
                return ""
            end

            local function wl_result_path()
                if not lfs or not lfs.writedir then
                    return nil
                end
                local dir = lfs.writedir() .. "Logs\\DcsWarLauncher\\"
                if lfs.mkdir then
                    pcall(lfs.mkdir, dir)
                end
                return dir .. WL.resultFileName
            end

            local function wl_write_result(final)
                if not io then
                    trigger.action.outText("WL result could not be written: io library unavailable.", 10)
                    return
                end

                local path = wl_result_path()
                if not path then
                    trigger.action.outText("WL result could not be written: lfs unavailable.", 10)
                    return
                end

                local file = io.open(path, "w")
                if not file then
                    trigger.action.outText("WL result could not be written: " .. path, 10)
                    return
                end

                file:write("{\n")
                file:write("  \"turn\": {{state.Turn}},\n")
                file:write("  \"final\": " .. tostring(final == true) .. ",\n")
                file:write("  \"elapsedSeconds\": " .. math.floor(timer.getTime() - WL.turnStartedAt) .. ",\n")
                file:write("  \"events\": [\n")
                for index, event in ipairs(WL.events) do
                    file:write("    {")
                    file:write("\"type\":\"" .. wl_escape(event.type) .. "\"")
                    file:write(",\"coalition\":\"" .. wl_escape(event.coalition) .. "\"")
                    file:write(",\"targetCoalition\":\"" .. wl_escape(event.targetCoalition) .. "\"")
                    file:write(",\"time\":" .. math.floor(event.time or 0))
                    file:write("}")
                    if index < #WL.events then
                        file:write(",")
                    end
                    file:write("\n")
                end
                file:write("  ]\n")
                file:write("}\n")
                file:close()
            end

            world.addEventHandler({
                onEvent = function(self, event)
                    local eventType = wl_event_type(event)
                    if not eventType then
                        return
                    end

                    WL.events[#WL.events + 1] = {
                        type = eventType,
                        coalition = wl_unit_coalition(event.initiator),
                        targetCoalition = wl_unit_coalition(event.target),
                        time = timer.getTime()
                    }
                end
            })

            local function wl_tick()
                local elapsed = timer.getTime() - WL.turnStartedAt
                local final = elapsed >= WL.turnDurationSeconds
                wl_write_result(final)

                if final and not WL.finalized then
                    WL.finalized = true
                    trigger.action.setUserFlag("WL_TURN_COMPLETE", 1)
                    trigger.action.outText("War Launcher turn complete. Mission will stop for campaign processing.", 30)
                    if net and net.dostring_in then
                        pcall(net.dostring_in, "server", "if net and net.stop_game then net.stop_game() end")
                    end
                    return nil
                end

                return timer.getTime() + 60
            end

            timer.scheduleFunction(wl_tick, {}, timer.getTime() + 60)
            trigger.action.outText("War Launcher 6h turn monitor active.", 10)
            """;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(invalid.Contains(character) ? '-' : character);
        }

        return builder.Length == 0 ? "campaign" : builder.ToString();
    }

    private static string ToLuaString(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal)}\"";
}

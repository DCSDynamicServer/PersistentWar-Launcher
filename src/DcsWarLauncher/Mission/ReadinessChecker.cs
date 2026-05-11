using DcsWarLauncher.Infrastructure;

namespace DcsWarLauncher.Mission;

public sealed class ReadinessChecker(
    StateStore stateStore,
    MissionTemplateInspector templateInspector,
    MissionPlanExporter missionPlanExporter,
    MissionResultImporter missionResultImporter)
{
    public async Task<ReadinessReport> CheckV008Async()
    {
        var items = new List<ReadinessItem>();
        var state = (await stateStore.LoadAsync()).Normalize();
        items.Add(Ok("Campaign State", $"Turn {state.Turn}, {state.Theater}, {state.TurnDurationHours}h."));

        var template = templateInspector.InspectLatest();
        if (!template.IsReadable)
        {
            items.Add(Error("Mission Template", template.Warnings.FirstOrDefault() ?? "No readable template .miz found."));
        }
        else
        {
            items.Add(Ok("Mission Template", $"{template.FileName}, {template.ClientSlotCount} client slots, {template.Anchors.Count} WL anchors."));
            if (template.ClientSlotCount == 0)
            {
                items.Add(Warn("Player Slots", "No client slots detected. For real tests the template should keep player slots."));
            }
        }

        var plan = missionPlanExporter.Preview(state);
        var missingObjectives = plan.TemplateBindings.MissingObjectiveAnchors.Count;
        var missingAirbases = plan.TemplateBindings.MissingAirbaseAnchors.Count;
        if (plan.TemplateBindings.ObjectiveAnchors.Count == 0)
        {
            items.Add(Error("Objective Anchors", "No objective anchors are bound to the campaign."));
        }
        else if (missingObjectives > 0 || missingAirbases > 0)
        {
            items.Add(Warn("Template Bindings", $"{missingObjectives} objective and {missingAirbases} airbase anchor bindings are still missing."));
        }
        else
        {
            items.Add(Ok("Template Bindings", "All objective and airbase anchors are bound."));
        }

        if (plan.FlightGroups.Count == 0)
        {
            items.Add(Error("AI Packages", "No AI flight groups planned. Prepare a smoke-test state or recover aircraft before the final DCS smoke test."));
        }
        else
        {
            items.Add(Ok("AI Packages", $"{plan.FlightGroups.Count} flight groups planned."));
        }

        if (plan.WarehousePatches.Count == 0)
        {
            items.Add(Warn("Warehouse Plan", "No warehouse supply plan exported."));
        }
        else
        {
            var mapped = plan.WarehousePatches.Count(patch => patch.DcsWarehouseId is not null);
            items.Add(Ok("Warehouse Plan", $"{plan.WarehousePatches.Count} exported to mission-plan.json, {mapped} mapped for later DCS warehouse use. MIZ warehouses stay unchanged in v0.08."));
        }

        var generated = missionPlanExporter.GetLatestGeneratedMission();
        var expectedGeneratedName = $"{SanitizeFileName(state.CampaignId)}-turn-{state.Turn:D4}.miz";
        if (!generated.Exists)
        {
            items.Add(Error("Generated MIZ", "No generated Turn-MIZ found yet. Use Mission -> Turn-MIZ vorbereiten."));
        }
        else if (!string.Equals(generated.MizFileName, expectedGeneratedName, StringComparison.OrdinalIgnoreCase))
        {
            items.Add(Error("Generated MIZ", $"Latest Turn-MIZ is {generated.MizFileName}, expected {expectedGeneratedName}. Prepare a fresh Turn-MIZ."));
        }
        else
        {
            items.Add(Ok("Generated MIZ", $"{generated.MizFileName} matches the active campaign turn."));
        }

        var result = missionResultImporter.GetLatestResultStatus();
        items.Add(result.Exists
            ? Ok("Mission Result", $"{result.FileName} can be imported.")
            : Warn("Mission Result", "No mission result found yet. This is normal before the first DCS smoke test."));

        var isReady = items.All(item => item.Status != "error");
        var summary = isReady
            ? "v0.08 is ready for the final DCS smoke test."
            : "v0.08 has blocking items before the final DCS smoke test.";

        return new ReadinessReport("v0.08", isReady, summary, DateTimeOffset.UtcNow, items);
    }

    public async Task<ReadinessReport> PrepareV008SmokeStateAsync()
    {
        var smokeState = DcsWarLauncher.Domain.WarState.CreateDefault() with
        {
            CampaignName = "v0.08 Smoke Test",
            Phase = "Planning",
            CurrentTurnStartedUtc = DateTimeOffset.UtcNow,
            CurrentTurnEndsUtc = DateTimeOffset.UtcNow.AddHours(6),
            TurnHistory = [],
            LastBattleReport = null,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        await stateStore.SaveAsync(smokeState);
        return await CheckV008Async();
    }

    private static ReadinessItem Ok(string name, string message) => new(name, "ok", message);

    private static ReadinessItem Warn(string name, string message) => new(name, "warn", message);

    private static ReadinessItem Error(string name, string message) => new(name, "error", message);

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(character => invalid.Contains(character) ? '-' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "campaign" : sanitized;
    }
}

public sealed record ReadinessReport(
    string Version,
    bool IsReady,
    string Summary,
    DateTimeOffset CheckedUtc,
    IReadOnlyCollection<ReadinessItem> Items);

public sealed record ReadinessItem(
    string Name,
    string Status,
    string Message);

using DcsWarLauncher.Campaign;
using DcsWarLauncher.Domain;
using DcsWarLauncher.Infrastructure;
using DcsWarLauncher.Mission;
using Microsoft.Extensions.Options;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<LauncherOptions>(builder.Configuration.GetSection("Launcher"));
builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection("Scheduler"));
builder.Services.AddSingleton<StateStore>();
builder.Services.AddSingleton<DcsProcessService>();
builder.Services.AddSingleton<TurnEngine>();
builder.Services.AddSingleton<MissionPlanExporter>();
builder.Services.AddSingleton<MissionTemplateInspector>();
builder.Services.AddSingleton<MissionResultImporter>();
builder.Services.AddSingleton<ReadinessChecker>();
builder.Services.AddSingleton<MissionDeploymentService>();
builder.Services.AddSingleton<TurnAutomationService>();
builder.Services.AddSingleton<TurnSchedulerState>();
builder.Services.AddSingleton<AutomationLogService>();
builder.Services.AddHostedService<TurnSchedulerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new
{
    name = "DCS Persistent War Launcher",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/api/server/status", (DcsProcessService dcs) => Results.Ok(dcs.GetStatus()));

app.MapGet("/api/server/config-check", (DcsProcessService dcs) => Results.Ok(dcs.GetConfigCheck()));

app.MapGet("/api/operations/status", async (
    StateStore store,
    DcsProcessService dcs,
    TurnSchedulerState scheduler) =>
{
    var now = DateTimeOffset.UtcNow;
    var state = await store.LoadAsync();
    var dcsStatus = dcs.GetStatus();
    var config = dcs.GetConfigCheck();
    var schedulerStatus = scheduler.GetSnapshot();
    var remaining = state.CurrentTurnEndsUtc - now;
    var staleAfterSeconds = Math.Max(90, schedulerStatus.PollSeconds * 3);
    var schedulerStale = schedulerStatus.Enabled &&
        (!schedulerStatus.LastCheckedUtc.HasValue ||
            now - schedulerStatus.LastCheckedUtc.Value > TimeSpan.FromSeconds(staleAfterSeconds));

    var warnings = new List<string>();
    if (!schedulerStatus.Enabled)
    {
        warnings.Add("Automation is paused.");
    }

    if (schedulerStale)
    {
        warnings.Add("Scheduler check is stale.");
    }

    if (!dcsStatus.IsRunning)
    {
        warnings.Add("DCS server is not running.");
    }

    if (!config.ServerSettingsMissionExists)
    {
        warnings.Add("serverSettings.lua does not point to an existing mission.");
    }

    if (state.CurrentTurnEndsUtc <= now && schedulerStatus.Enabled)
    {
        warnings.Add("Current turn is expired; automation should advance it shortly.");
    }

    var health = warnings.Count == 0 ? "OK" : schedulerStale || !config.ServerSettingsMissionExists ? "Warn" : "Check";

    return Results.Ok(new OperationsStatus(
        state.Turn,
        state.CurrentTurnEndsUtc,
        Math.Max(0, (int)remaining.TotalSeconds),
        state.CurrentTurnEndsUtc <= now,
        schedulerStatus.Enabled,
        schedulerStatus.IsProcessing,
        schedulerStale,
        schedulerStatus.LastCheckedUtc,
        schedulerStatus.LastRunUtc,
        schedulerStatus.LastMessage,
        dcsStatus.IsRunning,
        dcsStatus.ProcessId,
        config.ServerSettingsMissionPath ?? config.DeploymentTargetPath,
        config.ServerSettingsMissionExists,
        health,
        warnings,
        now));
});

app.MapGet("/api/scheduler/status", (TurnSchedulerState scheduler) => Results.Ok(scheduler.GetSnapshot()));

app.MapGet("/api/scheduler/log", (AutomationLogService log) => Results.Ok(log.GetSnapshot()));

app.MapPost("/api/scheduler/enabled", async (
    HttpContext context,
    TurnSchedulerState scheduler,
    AutomationLogService automationLog) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var request = await context.Request.ReadFromJsonAsync<SchedulerEnabledRequest>();
    if (request is null)
    {
        return Results.BadRequest(new { error = "Invalid scheduler payload." });
    }

    var status = scheduler.SetEnabled(request.Enabled);
    await automationLog.AppendAsync(request.Enabled
        ? "Scheduler enabled from UI."
        : "Scheduler paused from UI.");

    return Results.Ok(status);
});

app.MapPost("/api/scheduler/run-once", async (
    HttpContext context,
    IOptions<SchedulerOptions> schedulerOptions,
    TurnAutomationService automation) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var result = await automation.RunExpiredTurnAsync(schedulerOptions.Value);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/api/readiness/v008", async (ReadinessChecker readiness) =>
{
    var result = await readiness.CheckV008Async();
    return Results.Ok(result);
});

app.MapPost("/api/readiness/v008/prepare-smoke-state", async (HttpContext context, ReadinessChecker readiness) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var result = await readiness.PrepareV008SmokeStateAsync();
    return Results.Ok(result);
});

app.MapPost("/api/server/start", async (HttpContext context, DcsProcessService dcs) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var request = await context.Request.ReadFromJsonAsync<StartMissionRequest>() ?? new();
    var result = await dcs.StartAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/server/deploy-latest-and-start", async (
    HttpContext context,
    MissionPlanExporter exporter,
    MissionDeploymentService deployment,
    DcsProcessService dcs) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var latest = exporter.GetLatestGeneratedMission();
    if (!latest.Exists ||
        string.IsNullOrWhiteSpace(latest.MizFilePath) ||
        string.IsNullOrWhiteSpace(latest.MizFileName))
    {
        return Results.BadRequest(ActionResultDto.Fail("No prepared Turn-MIZ found."));
    }

    var deployResult = await deployment.DeployAsync(latest.MizFilePath, latest.MizFileName);
    if (!deployResult.Success || string.IsNullOrWhiteSpace(deployResult.MissionPath))
    {
        return Results.BadRequest(ActionResultDto.Fail($"Mission deploy failed: {deployResult.Message}"));
    }

    var startResult = await dcs.StartAsync(new StartMissionRequest(deployResult.MissionPath));
    if (!startResult.Success)
    {
        return Results.BadRequest(ActionResultDto.Fail($"Mission deployed, but DCS was not started: {startResult.Message}"));
    }

    return Results.Ok(ActionResultDto.Ok($"{deployResult.Message} {startResult.Message}"));
});

app.MapPost("/api/server/stop", async (HttpContext context, DcsProcessService dcs) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var result = await dcs.StopAsync();
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/api/war/state", async (StateStore store) => Results.Ok(await store.LoadAsync()));

app.MapPost("/api/war/state", async (HttpContext context, StateStore store) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var state = await context.Request.ReadFromJsonAsync<WarState>();
    if (state is null)
    {
        return Results.BadRequest(new { error = "Invalid war state payload." });
    }

    await store.SaveAsync(state with { UpdatedUtc = DateTimeOffset.UtcNow });
    return Results.Ok(await store.LoadAsync());
});

app.MapPost("/api/war/reset-default", async (HttpContext context, StateStore store) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var state = WarState.CreateDefault();
    await store.SaveAsync(state);
    return Results.Ok(await store.LoadAsync());
});

app.MapPost("/api/war/advance-turn", async (HttpContext context, StateStore store, TurnEngine turnEngine) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var report = await context.Request.ReadFromJsonAsync<BattleReport>() ?? BattleReport.Empty;
    var state = await store.LoadAsync();
    var nextState = turnEngine.Advance(state, report);
    await store.SaveAsync(nextState);
    return Results.Ok(nextState);
});

app.MapPost("/api/war/advance-turn/from-result", async (
    HttpContext context,
    StateStore store,
    TurnEngine turnEngine,
    MissionResultImporter importer) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    try
    {
        var imported = await importer.ImportLatestAsync();
        var state = await store.LoadAsync();
        var nextState = turnEngine.Advance(state, imported.BattleReport);
        await store.SaveAsync(nextState);
        return Results.Ok(new
        {
            imported.FileName,
            imported.FilePath,
            imported.ImportedUtc,
            imported.BattleReport,
            state = nextState
        });
    }
    catch (FileNotFoundException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (JsonException ex)
    {
        return Results.BadRequest(new { error = $"Mission result could not be parsed: {ex.Message}" });
    }
});

app.MapPost("/api/mission/export-plan", async (HttpContext context, StateStore store, MissionPlanExporter exporter) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var state = await store.LoadAsync();
    var result = await exporter.ExportAsync(state);
    return Results.Ok(result);
});

app.MapGet("/api/mission/preview-plan", async (HttpContext context, StateStore store, MissionPlanExporter exporter) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var state = await store.LoadAsync();
    var result = exporter.Preview(state);
    return Results.Ok(result);
});

app.MapGet("/api/mission/generated/latest", (MissionPlanExporter exporter) =>
{
    var result = exporter.GetLatestGeneratedMission();
    return Results.Ok(result);
});

app.MapPost("/api/mission/generated/deploy-latest", async (
    HttpContext context,
    MissionPlanExporter exporter,
    MissionDeploymentService deployment) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    var latest = exporter.GetLatestGeneratedMission();
    if (!latest.Exists ||
        string.IsNullOrWhiteSpace(latest.MizFilePath) ||
        string.IsNullOrWhiteSpace(latest.MizFileName))
    {
        return Results.BadRequest(new { error = "No prepared Turn-MIZ found." });
    }

    var result = await deployment.DeployAsync(latest.MizFilePath, latest.MizFileName);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/api/mission/results/latest", (MissionResultImporter importer) =>
{
    var result = importer.GetLatestResultStatus();
    return Results.Ok(result);
});

app.MapPost("/api/mission/results/import", async (HttpContext context, MissionResultImporter importer) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    try
    {
        var result = await importer.ImportLatestAsync();
        return Results.Ok(result);
    }
    catch (FileNotFoundException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (JsonException ex)
    {
        return Results.BadRequest(new { error = $"Mission result could not be parsed: {ex.Message}" });
    }
});

app.MapPost("/api/mission/prepare", async (HttpContext context, StateStore store, MissionPlanExporter exporter) =>
{
    if (!IsAuthorized(context, app.Configuration))
    {
        return Results.Unauthorized();
    }

    try
    {
        var state = await store.LoadAsync();
        var result = await exporter.PrepareMissionAsync(state);
        return Results.Ok(result);
    }
    catch (FileNotFoundException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/mission/template/inspect", (MissionTemplateInspector inspector) =>
{
    var result = inspector.InspectLatest();
    return Results.Ok(result);
});

app.Run();

static bool IsAuthorized(HttpContext context, IConfiguration configuration)
{
    var token = configuration["Launcher:RemoteToken"];
    if (string.IsNullOrWhiteSpace(token))
    {
        return false;
    }

    var header = context.Request.Headers.Authorization.ToString();
    return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        && string.Equals(header["Bearer ".Length..], token, StringComparison.Ordinal);
}

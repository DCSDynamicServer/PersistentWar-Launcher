using DcsWarLauncher.Campaign;
using DcsWarLauncher.Domain;
using DcsWarLauncher.Infrastructure;
using DcsWarLauncher.Mission;
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
builder.Services.AddSingleton<TurnSchedulerState>();
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

app.MapGet("/api/scheduler/status", (TurnSchedulerState scheduler) => Results.Ok(scheduler.GetSnapshot()));

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

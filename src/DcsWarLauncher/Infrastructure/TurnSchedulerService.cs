using DcsWarLauncher.Campaign;
using DcsWarLauncher.Domain;
using Microsoft.Extensions.Options;

namespace DcsWarLauncher.Infrastructure;

public sealed class TurnSchedulerService(
    IOptions<SchedulerOptions> options,
    StateStore store,
    TurnEngine turnEngine,
    DcsProcessService dcs,
    TurnSchedulerState state,
    ILogger<TurnSchedulerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var schedulerOptions = Normalize(options.Value);
        state.Configure(schedulerOptions);

        if (!schedulerOptions.Enabled)
        {
            state.MarkIdle("Scheduler disabled by configuration.");
            return;
        }

        logger.LogInformation("Turn scheduler started with {PollSeconds}s poll interval.", schedulerOptions.PollSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckTurnAsync(schedulerOptions, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(schedulerOptions.PollSeconds), stoppingToken);
        }
    }

    private async Task CheckTurnAsync(SchedulerOptions schedulerOptions, CancellationToken cancellationToken)
    {
        state.MarkChecked();

        if (!schedulerOptions.AdvanceWhenTurnExpired)
        {
            state.MarkIdle("Scheduler active; automatic turn advance disabled.");
            return;
        }

        var currentState = await store.LoadAsync();
        if (currentState.CurrentTurnEndsUtc > DateTimeOffset.UtcNow)
        {
            state.MarkIdle($"Waiting for turn {currentState.Turn} to end.");
            return;
        }

        state.MarkProcessing($"Advancing expired turn {currentState.Turn}.");
        logger.LogInformation("Advancing expired turn {Turn}.", currentState.Turn);

        if (schedulerOptions.AutoStopServer)
        {
            await dcs.StopIfRunningAsync();
        }

        var nextState = turnEngine.Advance(currentState, BattleReport.Empty);
        await store.SaveAsync(nextState);

        if (schedulerOptions.AutoStartServer)
        {
            var startResult = await dcs.StartAsync(new StartMissionRequest());
            if (!startResult.Success)
            {
                state.MarkIdle($"Turn {nextState.Turn} created; server not started: {startResult.Message}");
                return;
            }
        }

        state.MarkCompleted($"Turn {nextState.Turn} created.");
    }

    private static SchedulerOptions Normalize(SchedulerOptions options) => new()
    {
        Enabled = options.Enabled,
        PollSeconds = Math.Clamp(options.PollSeconds, 10, 3600),
        AutoStopServer = options.AutoStopServer,
        AutoStartServer = options.AutoStartServer,
        AdvanceWhenTurnExpired = options.AdvanceWhenTurnExpired
    };
}

public sealed class TurnSchedulerState
{
    private readonly object _gate = new();
    private SchedulerStatus _status = new(
        false,
        true,
        true,
        true,
        30,
        false,
        null,
        null,
        "Scheduler not started.");

    public void Configure(SchedulerOptions options)
    {
        lock (_gate)
        {
            _status = _status with
            {
                Enabled = options.Enabled,
                AdvanceWhenTurnExpired = options.AdvanceWhenTurnExpired,
                AutoStopServer = options.AutoStopServer,
                AutoStartServer = options.AutoStartServer,
                PollSeconds = options.PollSeconds
            };
        }
    }

    public void MarkChecked()
    {
        lock (_gate)
        {
            _status = _status with { LastCheckedUtc = DateTimeOffset.UtcNow };
        }
    }

    public void MarkProcessing(string message)
    {
        lock (_gate)
        {
            _status = _status with
            {
                IsProcessing = true,
                LastCheckedUtc = DateTimeOffset.UtcNow,
                LastMessage = message
            };
        }
    }

    public void MarkCompleted(string message)
    {
        lock (_gate)
        {
            _status = _status with
            {
                IsProcessing = false,
                LastRunUtc = DateTimeOffset.UtcNow,
                LastMessage = message
            };
        }
    }

    public void MarkIdle(string message)
    {
        lock (_gate)
        {
            _status = _status with
            {
                IsProcessing = false,
                LastMessage = message
            };
        }
    }

    public SchedulerStatus GetSnapshot()
    {
        lock (_gate)
        {
            return _status;
        }
    }
}

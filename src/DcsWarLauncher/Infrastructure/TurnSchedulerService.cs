using DcsWarLauncher.Campaign;
using DcsWarLauncher.Domain;
using Microsoft.Extensions.Options;

namespace DcsWarLauncher.Infrastructure;

public sealed class TurnSchedulerService(
    IOptions<SchedulerOptions> options,
    TurnAutomationService automation,
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

        state.MarkProcessing("Checking turn automation.");
        var result = await automation.RunExpiredTurnAsync(schedulerOptions, cancellationToken);
        if (!result.TurnAdvanced)
        {
            state.MarkIdle(result.Message);
            return;
        }

        logger.LogInformation("Automation result for turn {Turn}: {Message}", result.Turn, result.Message);

        if (!result.Success)
        {
            state.MarkIdle(result.Message);
            return;
        }

        state.MarkCompleted(result.Message);
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

namespace DcsWarLauncher.Domain;

public sealed record LauncherOptions(
    string DcsExecutablePath,
    string DefaultMissionPath,
    string StartArguments,
    string RemoteToken);

public sealed class SchedulerOptions
{
    public bool Enabled { get; init; }
    public int PollSeconds { get; init; } = 30;
    public bool AutoStopServer { get; init; } = true;
    public bool AutoStartServer { get; init; } = true;
    public bool AdvanceWhenTurnExpired { get; init; } = true;
}

public sealed record StartMissionRequest(string? MissionPath = null);

public sealed record DcsStatus(
    bool IsRunning,
    int? ProcessId,
    string DcsExecutablePath,
    string DefaultMissionPath,
    DateTimeOffset CheckedUtc);

public sealed record ActionResultDto(bool Success, string Message)
{
    public static ActionResultDto Ok(string message) => new(true, message);
    public static ActionResultDto Fail(string message) => new(false, message);
}

public sealed record SchedulerStatus(
    bool Enabled,
    bool AdvanceWhenTurnExpired,
    bool AutoStopServer,
    bool AutoStartServer,
    int PollSeconds,
    bool IsProcessing,
    DateTimeOffset? LastCheckedUtc,
    DateTimeOffset? LastRunUtc,
    string LastMessage);

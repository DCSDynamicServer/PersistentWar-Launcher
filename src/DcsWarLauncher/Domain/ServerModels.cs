namespace DcsWarLauncher.Domain;

public sealed record LauncherOptions(
    string DcsExecutablePath,
    string DefaultMissionPath,
    string StartArguments,
    string RemoteToken,
    string? ServerMissionDirectory = null,
    string? DeployedMissionFileName = null,
    string? ServerSettingsPath = null,
    bool CleanupOldTurnMissions = true);

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

public sealed record DcsConfigCheck(
    bool IsReady,
    bool DcsExecutableConfigured,
    bool DcsExecutableExists,
    bool DefaultMissionConfigured,
    bool DefaultMissionExists,
    bool StartArgumentsConfigured,
    bool StartArgumentsContainMissionPlaceholder,
    bool RemoteTokenConfigured,
    bool DeploymentTargetConfigured,
    bool DeploymentDirectoryExists,
    string DeploymentTargetPath,
    bool CleanupOldTurnMissions,
    bool ServerSettingsConfigured,
    bool ServerSettingsExists,
    bool PatchServerSettings,
    bool SchedulerEnabled,
    bool AutoStopServer,
    bool AutoStartServer,
    bool AdvanceWhenTurnExpired,
    string MissionStartMode,
    string DcsExecutablePath,
    string DefaultMissionPath,
    string StartArguments,
    string ServerSettingsPath,
    string ServerSettingsRoot,
    bool ServerSettingsHasListStartIndex,
    string? ServerSettingsMissionPath,
    bool ServerSettingsMissionExists,
    string Mode,
    IReadOnlyCollection<string> Warnings,
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

public sealed record TurnAutomationResult(
    bool Success,
    bool TurnAdvanced,
    int Turn,
    string Message,
    string? MissionPath = null,
    string? PreparedMissionPath = null,
    string? MissionResultFileName = null);

public sealed record MissionDeploymentResult(
    bool Success,
    string Message,
    string? MissionPath,
    int DeletedOldMissions,
    bool ServerSettingsPatched);

public sealed record AutomationLogSnapshot(
    bool Exists,
    string Path,
    IReadOnlyCollection<string> Lines);

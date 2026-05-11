using System.Diagnostics;
using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Infrastructure;

public sealed class DcsProcessService(IConfiguration configuration, ILogger<DcsProcessService> logger)
{
    private Process? _process;

    public DcsStatus GetStatus()
    {
        var configuredPath = configuration["Launcher:DcsExecutablePath"] ?? "";
        var isRunning = _process is { HasExited: false };

        return new DcsStatus(
            isRunning,
            isRunning ? _process!.Id : null,
            configuredPath,
            configuration["Launcher:DefaultMissionPath"] ?? "",
            DateTimeOffset.UtcNow);
    }

    public DcsConfigCheck GetConfigCheck()
    {
        var exePath = configuration["Launcher:DcsExecutablePath"] ?? "";
        var missionPath = configuration["Launcher:DefaultMissionPath"] ?? "";
        var startArguments = configuration["Launcher:StartArguments"] ?? "";
        var remoteToken = configuration["Launcher:RemoteToken"] ?? "";
        var serverMissionDirectory = configuration["Launcher:ServerMissionDirectory"] ?? "";
        var deployedMissionFileName = configuration["Launcher:DeployedMissionFileName"] ?? "persistent-war-current.miz";
        var cleanupOldTurnMissions = configuration.GetValue("Launcher:CleanupOldTurnMissions", true);
        var serverSettingsPath = configuration["Launcher:ServerSettingsPath"] ?? "";
        var patchServerSettings = configuration.GetValue("Launcher:PatchServerSettings", true);
        var schedulerEnabled = configuration.GetValue("Scheduler:Enabled", false);
        var autoStopServer = configuration.GetValue("Scheduler:AutoStopServer", true);
        var autoStartServer = configuration.GetValue("Scheduler:AutoStartServer", true);
        var advanceWhenTurnExpired = configuration.GetValue("Scheduler:AdvanceWhenTurnExpired", true);

        var warnings = new List<string>();
        var dcsExecutableConfigured = !string.IsNullOrWhiteSpace(exePath);
        var dcsExecutableExists = dcsExecutableConfigured && File.Exists(exePath);
        var defaultMissionConfigured = !string.IsNullOrWhiteSpace(missionPath);
        var defaultMissionExists = defaultMissionConfigured && File.Exists(missionPath);
        var startArgumentsConfigured = !string.IsNullOrWhiteSpace(startArguments);
        var startArgumentsContainMissionPlaceholder = startArguments.Contains("{mission}", StringComparison.Ordinal);
        var remoteTokenConfigured = !string.IsNullOrWhiteSpace(remoteToken) &&
            !remoteToken.Equals("change-me-before-remote-use", StringComparison.OrdinalIgnoreCase);
        var deploymentTargetPath = !string.IsNullOrWhiteSpace(serverMissionDirectory)
            ? Path.Combine(serverMissionDirectory, deployedMissionFileName)
            : missionPath;
        var deploymentTargetConfigured = !string.IsNullOrWhiteSpace(deploymentTargetPath);
        var deploymentDirectory = deploymentTargetConfigured ? Path.GetDirectoryName(deploymentTargetPath) : null;
        var deploymentDirectoryExists = !string.IsNullOrWhiteSpace(deploymentDirectory) && Directory.Exists(deploymentDirectory);
        var serverSettingsConfigured = !string.IsNullOrWhiteSpace(serverSettingsPath);
        var serverSettingsExists = serverSettingsConfigured && File.Exists(serverSettingsPath);

        if (!dcsExecutableConfigured)
        {
            warnings.Add("DCS executable path is not configured.");
        }
        else if (!dcsExecutableExists)
        {
            warnings.Add("DCS executable path does not exist.");
        }

        if (!defaultMissionConfigured)
        {
            warnings.Add("Default mission path is not configured.");
        }
        else if (!defaultMissionExists)
        {
            warnings.Add("Default mission path does not exist.");
        }

        if (!startArgumentsConfigured)
        {
            warnings.Add("DCS start arguments are not configured.");
        }
        else if (!startArgumentsContainMissionPlaceholder)
        {
            warnings.Add("DCS start arguments should contain {mission}.");
        }

        if (!remoteTokenConfigured)
        {
            warnings.Add("Remote token is not configured for real remote use.");
        }

        if (!deploymentTargetConfigured)
        {
            warnings.Add("Server mission deploy target is not configured.");
        }
        else if (!deploymentDirectoryExists)
        {
            warnings.Add("Server mission deploy directory does not exist yet.");
        }

        if (patchServerSettings && !serverSettingsConfigured)
        {
            warnings.Add("serverSettings.lua path is not configured; DCS missionList will not be patched.");
        }
        else if (patchServerSettings && !serverSettingsExists)
        {
            warnings.Add("serverSettings.lua does not exist yet; it will be created when deploying.");
        }

        if (schedulerEnabled && autoStartServer)
        {
            warnings.Add("Scheduler AutoStart is enabled. The launcher may start DCS automatically when a turn expires.");
        }

        var isReady = dcsExecutableExists &&
            defaultMissionExists &&
            startArgumentsConfigured &&
            startArgumentsContainMissionPlaceholder &&
            remoteTokenConfigured &&
            deploymentTargetConfigured;
        var mode = schedulerEnabled
            ? autoStartServer ? "Live automation" : "Safe automation"
            : "Manual";

        return new DcsConfigCheck(
            isReady,
            dcsExecutableConfigured,
            dcsExecutableExists,
            defaultMissionConfigured,
            defaultMissionExists,
            startArgumentsConfigured,
            startArgumentsContainMissionPlaceholder,
            remoteTokenConfigured,
            deploymentTargetConfigured,
            deploymentDirectoryExists,
            deploymentTargetPath,
            cleanupOldTurnMissions,
            serverSettingsConfigured,
            serverSettingsExists,
            patchServerSettings,
            schedulerEnabled,
            autoStopServer,
            autoStartServer,
            advanceWhenTurnExpired,
            mode,
            warnings,
            DateTimeOffset.UtcNow);
    }

    public Task<ActionResultDto> StartAsync(StartMissionRequest request)
    {
        if (_process is { HasExited: false })
        {
            return Task.FromResult(ActionResultDto.Fail("DCS is already running."));
        }

        var exePath = configuration["Launcher:DcsExecutablePath"];
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return Task.FromResult(ActionResultDto.Fail("DCS executable path is not configured or does not exist."));
        }

        var missionPath = string.IsNullOrWhiteSpace(request.MissionPath)
            ? configuration["Launcher:DefaultMissionPath"]
            : request.MissionPath;

        if (string.IsNullOrWhiteSpace(missionPath) || !File.Exists(missionPath))
        {
            return Task.FromResult(ActionResultDto.Fail("Mission path is not configured or does not exist."));
        }

        var argsTemplate = configuration["Launcher:StartArguments"] ?? "--server --norender -w DCS.openbeta \"{mission}\"";
        var args = argsTemplate.Replace("{mission}", missionPath, StringComparison.Ordinal);

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory,
            UseShellExecute = false
        };

        _process = Process.Start(startInfo);
        logger.LogInformation("Started DCS process {ProcessId} with mission {MissionPath}", _process?.Id, missionPath);

        return Task.FromResult(_process is null
            ? ActionResultDto.Fail("Failed to start DCS process.")
            : ActionResultDto.Ok($"Started DCS process {_process.Id}."));
    }

    public Task<ActionResultDto> StopAsync()
    {
        if (_process is null || _process.HasExited)
        {
            return Task.FromResult(ActionResultDto.Fail("No tracked DCS process is running."));
        }

        _process.Kill(entireProcessTree: true);
        logger.LogInformation("Stopped DCS process {ProcessId}", _process.Id);
        return Task.FromResult(ActionResultDto.Ok("Stopped DCS process."));
    }

    public Task<ActionResultDto> StopIfRunningAsync()
    {
        if (_process is null || _process.HasExited)
        {
            return Task.FromResult(ActionResultDto.Ok("DCS process was not running."));
        }

        return StopAsync();
    }
}

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

using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Infrastructure;

public sealed class MissionDeploymentService(IConfiguration configuration, ILogger<MissionDeploymentService> logger)
{
    private static readonly string[] CleanupPatterns =
    [
        "campaign-*-turn-*.miz",
        "war-launcher-turn-*.miz",
        "war-launcher-current-*.miz"
    ];

    public Task<MissionDeploymentResult> DeployAsync(string sourceMissionPath, string sourceMissionFileName)
    {
        if (!File.Exists(sourceMissionPath))
        {
            return Task.FromResult(new MissionDeploymentResult(
                false,
                "Prepared Turn-MIZ does not exist.",
                null,
                0));
        }

        var targetPath = ResolveDeploymentTarget(sourceMissionFileName);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return Task.FromResult(new MissionDeploymentResult(
                true,
                "No server mission deploy target configured; using generated Turn-MIZ directly.",
                sourceMissionPath,
                0));
        }

        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            return Task.FromResult(new MissionDeploymentResult(
                false,
                "Server mission deploy target has no directory.",
                null,
                0));
        }

        Directory.CreateDirectory(targetDirectory);
        var deleted = CleanupOldTurnMissions(targetDirectory, targetPath);
        File.Copy(sourceMissionPath, targetPath, overwrite: true);
        logger.LogInformation("Deployed mission {SourceMission} to {TargetMission}", sourceMissionPath, targetPath);

        return Task.FromResult(new MissionDeploymentResult(
            true,
            $"Deployed Turn-MIZ to {targetPath}; removed {deleted} old Turn-MIZ file(s).",
            targetPath,
            deleted));
    }

    public string ResolveDeploymentTarget(string sourceMissionFileName = "persistent-war-current.miz")
    {
        var serverMissionDirectory = configuration["Launcher:ServerMissionDirectory"];
        if (!string.IsNullOrWhiteSpace(serverMissionDirectory))
        {
            var fileName = configuration["Launcher:DeployedMissionFileName"];
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "persistent-war-current.miz";
            }

            return Path.Combine(serverMissionDirectory, fileName);
        }

        var defaultMissionPath = configuration["Launcher:DefaultMissionPath"];
        return string.IsNullOrWhiteSpace(defaultMissionPath)
            ? ""
            : defaultMissionPath;
    }

    private int CleanupOldTurnMissions(string targetDirectory, string targetPath)
    {
        if (!configuration.GetValue("Launcher:CleanupOldTurnMissions", true))
        {
            return 0;
        }

        var deleted = 0;
        var targetFullPath = Path.GetFullPath(targetPath);
        foreach (var pattern in CleanupPatterns)
        {
            foreach (var file in Directory.GetFiles(targetDirectory, pattern, SearchOption.TopDirectoryOnly))
            {
                if (Path.GetFullPath(file).Equals(targetFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Delete(file);
                deleted++;
            }
        }

        return deleted;
    }
}

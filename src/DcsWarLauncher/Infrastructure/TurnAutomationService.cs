using System.Text.Json;
using DcsWarLauncher.Campaign;
using DcsWarLauncher.Domain;
using DcsWarLauncher.Mission;

namespace DcsWarLauncher.Infrastructure;

public sealed class TurnAutomationService(
    StateStore store,
    TurnEngine turnEngine,
    MissionPlanExporter missionPlanExporter,
    MissionResultImporter missionResultImporter,
    MissionDeploymentService missionDeployment,
    DcsProcessService dcs,
    ILogger<TurnAutomationService> logger)
{
    public async Task<TurnAutomationResult> RunExpiredTurnAsync(
        SchedulerOptions options,
        CancellationToken cancellationToken = default)
    {
        var currentState = await store.LoadAsync();
        if (currentState.CurrentTurnEndsUtc > DateTimeOffset.UtcNow)
        {
            return new TurnAutomationResult(
                true,
                false,
                currentState.Turn,
                $"Waiting for turn {currentState.Turn} to end.");
        }

        logger.LogInformation("Running automation for expired turn {Turn}.", currentState.Turn);

        BattleReport battleReport;
        string? resultFileName;
        try
        {
            (battleReport, resultFileName) = await LoadBattleReportAsync(cancellationToken);
        }
        catch (JsonException ex)
        {
            return new TurnAutomationResult(
                false,
                false,
                currentState.Turn,
                $"Turn {currentState.Turn} not advanced; mission result could not be parsed: {ex.Message}");
        }

        if (options.AutoStopServer)
        {
            await dcs.StopIfRunningAsync();
        }

        var nextState = turnEngine.Advance(currentState, battleReport);

        PreparedMissionResult preparedMission;
        try
        {
            preparedMission = await missionPlanExporter.PrepareMissionAsync(nextState);
        }
        catch (FileNotFoundException ex)
        {
            return new TurnAutomationResult(
                false,
                false,
                currentState.Turn,
                $"Turn {currentState.Turn} not advanced; mission could not be prepared: {ex.Message}",
                MissionResultFileName: resultFileName);
        }

        var deployment = await missionDeployment.DeployAsync(preparedMission.MizFilePath, preparedMission.MizFileName);
        if (!deployment.Success || string.IsNullOrWhiteSpace(deployment.MissionPath))
        {
            return new TurnAutomationResult(
                false,
                false,
                currentState.Turn,
                $"Turn {currentState.Turn} not advanced; mission could not be deployed: {deployment.Message}",
                PreparedMissionPath: preparedMission.MizFilePath,
                MissionResultFileName: resultFileName);
        }

        await store.SaveAsync(nextState);

        if (options.AutoStartServer)
        {
            var startResult = await dcs.StartAsync(new StartMissionRequest(deployment.MissionPath));
            if (!startResult.Success)
            {
                return new TurnAutomationResult(
                    false,
                    true,
                    nextState.Turn,
                    $"Turn {nextState.Turn} created and MIZ prepared; server not started: {startResult.Message}",
                    deployment.MissionPath,
                    preparedMission.MizFilePath,
                    resultFileName);
            }
        }

        var source = resultFileName is null ? "empty battle report" : resultFileName;
        return new TurnAutomationResult(
            true,
            true,
            nextState.Turn,
            $"Turn {nextState.Turn} created; prepared {preparedMission.MizFileName} from {source}. {deployment.Message}",
            deployment.MissionPath,
            preparedMission.MizFilePath,
            resultFileName);
    }

    private async Task<(BattleReport BattleReport, string? FileName)> LoadBattleReportAsync(CancellationToken cancellationToken)
    {
        var resultStatus = missionResultImporter.GetLatestResultStatus();
        if (!resultStatus.Exists)
        {
            return (BattleReport.Empty, null);
        }

        try
        {
            var imported = await missionResultImporter.ImportLatestAsync();
            return (imported.BattleReport, imported.FileName);
        }
        catch (FileNotFoundException)
        {
            return (BattleReport.Empty, null);
        }
    }
}

namespace DcsWarLauncher.Domain;

public sealed record MissionPackageState(
    string Id,
    string Coalition,
    string Task,
    string Target,
    string Status,
    int AircraftCount,
    string Squadron);

public sealed record BattleReport(
    int BlueMissionSuccess,
    int RedMissionSuccess,
    int BlueLosses,
    int RedLosses,
    int AirSuperiority)
{
    public static BattleReport Empty => new(0, 0, 0, 0, 0);
}

public sealed record AiOrder(string Coalition, string Task, string Target, int Confidence);

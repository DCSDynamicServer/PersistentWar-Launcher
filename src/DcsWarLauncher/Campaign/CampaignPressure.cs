using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public sealed record CampaignPressure(int BluePressure, int RedPressure)
{
    public int NetPressure => BluePressure - RedPressure;

    public static CampaignPressure From(BattleReport report)
    {
        var bluePressure = Clamp(report.BlueMissionSuccess - report.BlueLosses + report.AirSuperiority);
        var redPressure = Clamp(report.RedMissionSuccess - report.RedLosses - report.AirSuperiority);
        return new CampaignPressure(bluePressure, redPressure);
    }

    private static int Clamp(int value) => Math.Clamp(value, -25, 25);
}

using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public static class SquadronEngine
{
    public static SquadronState AdvanceSquadron(SquadronState squadron, BattleReport report)
    {
        var losses = squadron.Coalition == "blue" ? report.BlueLosses : report.RedLosses;
        var repaired = Math.Max(1, squadron.AircraftTotal / 8);
        var ready = Math.Clamp(squadron.AircraftReady - losses / 3 + repaired, 0, squadron.AircraftTotal);
        var readiness = Math.Clamp(squadron.PilotReadiness - losses + 3, 0, 100);

        return squadron with
        {
            AircraftReady = ready,
            PilotReadiness = readiness
        };
    }
}

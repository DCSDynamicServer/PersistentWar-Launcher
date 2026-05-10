namespace DcsWarLauncher.Domain;

public sealed record SquadronState(
    string Name,
    string Coalition,
    string Aircraft,
    string HomeBase,
    int AircraftTotal,
    int AircraftReady,
    int PilotReadiness);

public sealed record GroundUnitState(
    string Name,
    string Coalition,
    string Type,
    string Location,
    int Strength,
    int Supply,
    int Readiness,
    string Posture);

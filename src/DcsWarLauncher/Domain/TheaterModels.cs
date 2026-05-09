namespace DcsWarLauncher.Domain;

public sealed record ObjectiveState(string Name, string Owner, int Strength);

public sealed record AirbaseState(
    string Name,
    string Owner,
    int RunwayHealth,
    int Fuel,
    int X,
    int Y,
    string Status);

public sealed record FrontlineSegment(string Name, int StartX, int StartY, int EndX, int EndY, string Momentum);

namespace DcsWarLauncher.Domain;

public sealed record SupplyDepotState(
    string Name,
    string Coalition,
    string Location,
    int Stores,
    int X,
    int Y,
    string Status);

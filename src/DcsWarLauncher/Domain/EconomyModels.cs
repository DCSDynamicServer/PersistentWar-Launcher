namespace DcsWarLauncher.Domain;

public sealed record SupplyDepotState(
    string Name,
    string Coalition,
    string Location,
    int Stores,
    int X,
    int Y,
    string Status);

public sealed record FactoryState(
    string Name,
    string Coalition,
    string Location,
    string OutputType,
    int Health,
    int Production,
    string Status);

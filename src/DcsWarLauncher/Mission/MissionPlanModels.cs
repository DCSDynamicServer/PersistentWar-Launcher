using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Mission;

public sealed record MissionPlan(
    string CampaignId,
    string CampaignName,
    string Theater,
    int Turn,
    DateTimeOffset GeneratedUtc,
    TemplatePolicy TemplatePolicy,
    IReadOnlyCollection<AirbaseState> Airbases,
    IReadOnlyCollection<ObjectiveState> Objectives,
    IReadOnlyCollection<FrontlineMarkerPlan> FrontlineMarkers,
    IReadOnlyCollection<FlightGroupPlan> FlightGroups,
    IReadOnlyCollection<GroundGroupPlan> GroundGroups,
    IReadOnlyCollection<CampaignTargetPlan> SupplyTargets,
    IReadOnlyCollection<CampaignTargetPlan> FactoryTargets);

public sealed record TemplatePolicy(
    string Mode,
    string PreserveGroupNameContains,
    string Notes);

public sealed record FrontlineMarkerPlan(
    string Id,
    string Name,
    int StartX,
    int StartY,
    int EndX,
    int EndY,
    string Momentum);

public sealed record FlightGroupPlan(
    string Id,
    string Coalition,
    string Task,
    string Target,
    string Squadron,
    int AircraftCount,
    string Status);

public sealed record GroundGroupPlan(
    string Id,
    string Name,
    string Coalition,
    string Type,
    string Location,
    int Strength,
    int Supply,
    int Readiness,
    string Posture);

public sealed record CampaignTargetPlan(
    string Id,
    string Name,
    string Coalition,
    string Location,
    string TargetType,
    string Status,
    int Value);

public sealed record MissionExportResult(
    string FileName,
    string FilePath,
    int Turn,
    DateTimeOffset ExportedUtc,
    int PackageCount,
    int GroundGroupCount,
    int TargetCount);

public sealed record PreparedMissionResult(
    string MizFileName,
    string MizFilePath,
    string MissionPlanFileName,
    string MissionPlanFilePath,
    int Turn,
    DateTimeOffset PreparedUtc,
    string TemplateFileName);

public sealed record MissionTemplateInspection(
    string FileName,
    string FilePath,
    bool IsReadable,
    string Theater,
    IReadOnlyCollection<string> MissingFiles,
    IReadOnlyCollection<ClientGroupInspection> ClientGroups,
    IReadOnlyCollection<string> Warnings)
{
    public int ClientSlotCount => ClientGroups.Sum(group => group.ClientUnits);
}

public sealed record ClientGroupInspection(
    string Name,
    string Coalition,
    string Aircraft,
    int ClientUnits,
    int AiUnits,
    IReadOnlyCollection<int> AirdromeIds);

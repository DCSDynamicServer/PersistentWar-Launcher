using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Mission;

public sealed record MissionPlan(
    string CampaignId,
    string CampaignName,
    string Theater,
    int Turn,
    DateTimeOffset GeneratedUtc,
    TemplatePolicy TemplatePolicy,
    TemplateBindings TemplateBindings,
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

public sealed record TemplateBindings(
    IReadOnlyCollection<ObjectiveAnchorBinding> ObjectiveAnchors,
    IReadOnlyCollection<AirbaseAnchorBinding> AirbaseAnchors,
    IReadOnlyCollection<FrontAnchorBinding> FrontAnchors,
    IReadOnlyCollection<MissingObjectiveAnchor> MissingObjectiveAnchors,
    IReadOnlyCollection<MissingAirbaseAnchor> MissingAirbaseAnchors);

public sealed record ObjectiveAnchorBinding(
    string Objective,
    string Coalition,
    string AnchorName,
    double X,
    double Y,
    double Radius);

public sealed record FrontAnchorBinding(
    string AnchorName,
    int Sequence,
    double X,
    double Y,
    double Radius);

public sealed record AirbaseAnchorBinding(
    string Airbase,
    string AnchorType,
    string AnchorName,
    double X,
    double Y,
    double Radius);

public sealed record MissingObjectiveAnchor(
    string Objective,
    string Coalition,
    IReadOnlyCollection<string> ExpectedAnchorNames);

public sealed record MissingAirbaseAnchor(
    string Airbase,
    string AnchorType,
    IReadOnlyCollection<string> ExpectedAnchorNames);

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
    string Aircraft,
    int AircraftCount,
    string Status,
    string? DepartureAnchor,
    string? TargetAnchor,
    IReadOnlyCollection<RouteWaypointPlan> Route);

public sealed record RouteWaypointPlan(
    string Name,
    string Role,
    double X,
    double Y);

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

public sealed record GeneratedMissionStatus(
    string? MizFileName,
    string? MizFilePath,
    long? SizeBytes,
    DateTimeOffset? LastModifiedUtc,
    bool Exists);

public sealed record MissionTemplateInspection(
    string FileName,
    string FilePath,
    bool IsReadable,
    string Theater,
    IReadOnlyCollection<string> MissingFiles,
    IReadOnlyCollection<ClientGroupInspection> ClientGroups,
    IReadOnlyCollection<TemplateAnchorInspection> Anchors,
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

public sealed record TemplateAnchorInspection(
    string Name,
    string Kind,
    double? X,
    double? Y,
    double? Radius);

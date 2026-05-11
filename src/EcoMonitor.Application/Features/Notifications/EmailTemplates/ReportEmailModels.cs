namespace EcoMonitor.Application.Features.Notifications.EmailTemplates;

public sealed record ReportCreatedEmailModel(
    string ReporterName,
    Guid ReportId,
    string Description,
    DateTime CreatedAt);

public sealed record ReportConfirmedEmailModel(
    string ReporterName,
    Guid ReportId,
    string InspectorName,
    DateTime ConfirmedAt);

public sealed record ReportRejectedEmailModel(
    string ReporterName,
    Guid ReportId,
    string InspectorName,
    string Reason,
    DateTime RejectedAt);

public sealed record ReportResolvedEmailModel(
    string ReporterName,
    Guid ReportId,
    string InspectorName,
    string Notes,
    DateTime ResolvedAt);

public sealed record CleanupStartedEmailModel(
    string ReporterName,
    Guid ReportId,
    DateTime StartedAt);

public sealed record CleanupCompletedEmailModel(
    string ReporterName,
    Guid ReportId,
    DateTime CompletedAt,
    string CleanupNotes);

public sealed record InspectorNewAssignmentEmailModel(
    string InspectorName,
    Guid ReportId,
    string ReportDescription,
    DateTime ReportedAt,
    string ReportUrl);

public sealed record CleanupCrewNewTaskEmailModel(
    string CrewName,
    Guid ReportId,
    string ReportDescription,
    double Latitude,
    double Longitude,
    string ReportUrl);

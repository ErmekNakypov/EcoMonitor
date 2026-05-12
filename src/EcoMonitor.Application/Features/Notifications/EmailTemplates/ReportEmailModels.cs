namespace EcoMonitor.Application.Features.Notifications.EmailTemplates;

public sealed record ReportCreatedEmailModel(
    string ReporterName,
    Guid ReportId,
    string Description,
    DateTime CreatedAt,
    bool WasAutoConfirmed = false,
    string? AutoTriageReason = null);

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
    string ReportUrl,
    string? AutoTriageReason = null);

public sealed record CleanupCrewNewTaskEmailModel(
    string CrewName,
    Guid ReportId,
    string ReportDescription,
    double Latitude,
    double Longitude,
    string ReportUrl);

public sealed record AppealFiledEmailModel(
    string ReporterName,
    Guid ReportId,
    string AppealReason,
    DateTime AppealedAt);

public sealed record AppealUpheldEmailModel(
    string ReporterName,
    Guid ReportId,
    string InspectorName,
    string InspectorNotes,
    DateTime ReviewedAt);

public sealed record AppealDismissedEmailModel(
    string ReporterName,
    Guid ReportId,
    string InspectorName,
    string InspectorNotes,
    DateTime ReviewedAt);

public sealed record InspectorAppealReceivedEmailModel(
    string InspectorName,
    Guid ReportId,
    string ReportDescription,
    string AppealReason,
    DateTime AppealedAt,
    string ReportUrl);

public sealed record InspectorReportFlaggedEmailModel(
    string InspectorName,
    Guid ReportId,
    string ReportDescription,
    string CrewName,
    string FlagReasonDisplay,
    string? AdditionalNotes,
    DateTime FlaggedAt,
    string ReportUrl);

public sealed record CleanupCrewReportReturnedEmailModel(
    string CrewName,
    Guid ReportId,
    string ReportDescription,
    string InspectorName,
    string InspectorNotes,
    string ReportUrl);

public sealed record CleanupCrewReportReassignedEmailModel(
    string CrewName,
    Guid ReportId,
    string ReportDescription,
    string InspectorName,
    string InspectorNotes,
    int ReassignCount,
    string ReportUrl);

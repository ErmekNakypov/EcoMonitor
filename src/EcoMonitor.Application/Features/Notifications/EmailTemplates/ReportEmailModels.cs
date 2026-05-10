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

namespace EcoMonitor.Application.Features.Notifications;

public interface IRoleNotificationService
{
    Task NotifyInspectorsOfNewReportAsync(Guid reportId, CancellationToken ct = default);
    // Sends the new-report email to a single inspector — used when the report
    // was auto-assigned via district. Falls back to broadcast if the inspector
    // is missing or inactive.
    Task NotifyInspectorOfNewReviewTaskAsync(Guid reportId, Guid inspectorId, CancellationToken ct = default);
    Task NotifyCleanupCrewOfNewTaskAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyInspectorsOfAppealAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyInspectorsOfFlaggedReportAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyCleanupCrewOfReturnedReportAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyCleanupCrewOfReassignedReportAsync(Guid reportId, Guid? excludedCrewId, CancellationToken ct = default);
}

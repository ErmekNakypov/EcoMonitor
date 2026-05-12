namespace EcoMonitor.Application.Features.Notifications;

public interface IRoleNotificationService
{
    Task NotifyInspectorsOfNewReportAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyCleanupCrewOfNewTaskAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyInspectorsOfAppealAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyInspectorsOfFlaggedReportAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyCleanupCrewOfReturnedReportAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyCleanupCrewOfReassignedReportAsync(Guid reportId, Guid? excludedCrewId, CancellationToken ct = default);
}

namespace EcoMonitor.Application.Features.Notifications;

public interface IRoleNotificationService
{
    Task NotifyInspectorsOfNewReportAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyCleanupCrewOfNewTaskAsync(Guid reportId, CancellationToken ct = default);
}

namespace EcoMonitor.Application.Features.Notifications;

public interface IReportNotificationService
{
    Task NotifyReportCreatedAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyReportConfirmedAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyReportRejectedAsync(Guid reportId, CancellationToken ct = default);
    Task NotifyReportResolvedAsync(Guid reportId, CancellationToken ct = default);
}

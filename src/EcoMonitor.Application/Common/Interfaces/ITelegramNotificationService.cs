namespace EcoMonitor.Application.Common.Interfaces;

public interface ITelegramNotificationService
{
    Task NotifyAsync(Guid reportId, ReportStatusNotification kind, CancellationToken ct = default);
}

public enum ReportStatusNotification
{
    Confirmed,
    Rejected,
    CleanupStarted,
    CleanupCompleted,
    Resolved
}

namespace EcoMonitor.Application.Common.Interfaces;

public interface IEmailQueue
{
    Task EnqueueAsync(
        string toAddress,
        string toName,
        string subject,
        string htmlBody,
        string templateName,
        Guid? relatedEntityId = null,
        CancellationToken ct = default);
}

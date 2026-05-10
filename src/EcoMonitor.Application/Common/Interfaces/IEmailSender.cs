using EcoMonitor.Domain.Entities;

namespace EcoMonitor.Application.Common.Interfaces;

public interface IEmailSender
{
    Task<bool> TrySendAsync(EmailMessage message, CancellationToken ct = default);
}

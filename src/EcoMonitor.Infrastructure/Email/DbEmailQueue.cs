using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Persistence;

namespace EcoMonitor.Infrastructure.Email;

public sealed class DbEmailQueue : IEmailQueue
{
    private readonly ApplicationDbContext _db;

    public DbEmailQueue(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task EnqueueAsync(
        string toAddress,
        string toName,
        string subject,
        string htmlBody,
        string templateName,
        Guid? relatedEntityId = null,
        CancellationToken ct = default)
    {
        var msg = new EmailMessage
        {
            ToAddress = toAddress,
            ToName = toName,
            Subject = subject,
            HtmlBody = htmlBody,
            TemplateName = templateName,
            RelatedEntityId = relatedEntityId,
            Status = EmailStatus.Pending,
            NextAttemptAt = DateTime.UtcNow
        };

        _db.EmailMessages.Add(msg);
        await _db.SaveChangesAsync(ct);
    }
}

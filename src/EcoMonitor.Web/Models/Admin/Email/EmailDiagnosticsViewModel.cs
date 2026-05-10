using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Web.Models.Admin.Email;

public class EmailDiagnosticsViewModel
{
    public bool IsConfigured { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    public string Username { get; set; } = string.Empty;
    public string MaskedPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int MaxRetryAttempts { get; set; }
    public int RetryDelayMinutes { get; set; }

    public int PendingCount7d { get; set; }
    public int SentCount7d { get; set; }
    public int FailedCount7d { get; set; }
    public int FailedTotal { get; set; }

    public IReadOnlyList<RecentEmailViewModel> Recent { get; set; } = Array.Empty<RecentEmailViewModel>();
}

public class RecentEmailViewModel
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
}

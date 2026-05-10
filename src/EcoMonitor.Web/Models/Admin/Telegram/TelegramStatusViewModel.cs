namespace EcoMonitor.Web.Models.Admin.Telegram;

public class TelegramStatusViewModel
{
    public bool IsConfigured { get; set; }
    public bool IsConnected { get; set; }
    public string? ConnectionError { get; set; }
    public string? BotUsername { get; set; }
    public long? BotId { get; set; }

    public int TotalTelegramReports { get; set; }
    public int UniqueTelegramUsers { get; set; }

    public IReadOnlyList<TelegramRecentUserViewModel> RecentUsers { get; set; } = Array.Empty<TelegramRecentUserViewModel>();
}

public class TelegramRecentUserViewModel
{
    public long TelegramUserId { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public DateTime LastInteractionAt { get; set; }
    public int TotalReports { get; set; }
}

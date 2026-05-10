using EcoMonitor.Domain.Common;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Domain.Entities;

public class TelegramUserSession : BaseEntity
{
    public long TelegramUserId { get; set; }
    public string? FirstName { get; set; }
    public string? UserName { get; set; }
    public TelegramSessionState State { get; set; } = TelegramSessionState.Idle;
    public string Language { get; set; } = "ru";
    public string? DraftDescription { get; set; }
    public List<string> DraftPhotoFileIds { get; set; } = new();
    public double? DraftLatitude { get; set; }
    public double? DraftLongitude { get; set; }
    public DateTime LastInteractionAt { get; set; } = DateTime.UtcNow;
}

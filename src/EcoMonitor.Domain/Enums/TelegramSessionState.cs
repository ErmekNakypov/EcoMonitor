namespace EcoMonitor.Domain.Enums;

public enum TelegramSessionState
{
    Idle = 0,
    AwaitingDescription = 1,
    AwaitingPhotos = 2,
    AwaitingLocation = 3,
    AwaitingConfirmation = 4
}

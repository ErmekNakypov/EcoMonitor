using Telegram.Bot;
using Telegram.Bot.Types;

namespace EcoMonitor.Application.Common.Interfaces;

public interface ITelegramDialogService
{
    Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct);
}

using EcoMonitor.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EcoMonitor.Infrastructure.Telegram;

public sealed class TelegramBotHostedService : BackgroundService
{
    private readonly ILogger<TelegramBotHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private TelegramBotClient? _botClient;

    public TelegramBotHostedService(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<TelegramBotHostedService> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var token = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Telegram:BotToken is not configured. Bot will not start.");
            return;
        }

        _botClient = new TelegramBotClient(token);

        try
        {
            var me = await _botClient.GetMe(stoppingToken);
            _logger.LogInformation("Telegram bot started: @{Username} ({Id})", me.Username, me.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Telegram. Bot will not start.");
            return;
        }

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            DropPendingUpdates = true
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dialogService = scope.ServiceProvider.GetRequiredService<ITelegramDialogService>();
            await dialogService.HandleUpdateAsync(bot, update, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Telegram update {UpdateId}", update.Id);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Telegram polling error");
        return Task.CompletedTask;
    }
}

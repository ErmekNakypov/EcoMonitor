using System.Globalization;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace EcoMonitor.Infrastructure.Telegram;

public sealed class TelegramDialogService : ITelegramDialogService
{
    private const int DescriptionMin = 10;
    private const int DescriptionMax = 1000;
    private const int MaxPhotos = 5;

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly BotLocalizer _localizer;
    private readonly ILogger<TelegramDialogService> _logger;

    public TelegramDialogService(
        ApplicationDbContext db,
        IWebHostEnvironment env,
        BotLocalizer localizer,
        ILogger<TelegramDialogService> logger)
    {
        _db = db;
        _env = env;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.CallbackQuery is { } callback)
        {
            await HandleCallbackAsync(bot, callback, ct);
            return;
        }

        var message = update.Message;
        if (message?.From is null)
        {
            return;
        }

        var (session, isNew) = await GetOrCreateSessionAsync(message.From, ct);
        session.LastInteractionAt = DateTime.UtcNow;

        var text = message.Text?.Trim();
        if (!string.IsNullOrEmpty(text) && text.StartsWith('/'))
        {
            await HandleCommandAsync(bot, message, session, isNew, ct);
            await _db.SaveChangesAsync(ct);
            return;
        }

        switch (session.State)
        {
            case TelegramSessionState.AwaitingDescription:
                await HandleDescriptionAsync(bot, message, session, ct);
                break;
            case TelegramSessionState.AwaitingPhotos:
                await HandlePhotosAsync(bot, message, session, ct);
                break;
            case TelegramSessionState.AwaitingLocation:
                await HandleLocationAsync(bot, message, session, ct);
                break;
            case TelegramSessionState.AwaitingConfirmation:
                await HandleConfirmationAsync(bot, message, session, ct);
                break;
            default:
                await SendIdleHelpAsync(bot, message, session, ct);
                break;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleCallbackAsync(ITelegramBotClient bot, CallbackQuery callback, CancellationToken ct)
    {
        if (callback.From is null)
        {
            return;
        }

        var (session, _) = await GetOrCreateSessionAsync(callback.From, ct);
        session.LastInteractionAt = DateTime.UtcNow;

        var data = callback.Data ?? string.Empty;

        if (data.StartsWith("lang:", StringComparison.Ordinal))
        {
            var requested = data.Substring("lang:".Length);
            if (BotLocalizer.IsSupported(requested))
            {
                session.Language = requested;
                await _db.SaveChangesAsync(ct);

                await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);

                if (callback.Message is not null)
                {
                    await bot.SendMessage(
                        callback.Message.Chat.Id,
                        _localizer.Get(session.Language, "language_set_ok"),
                        cancellationToken: ct);
                }
                return;
            }
        }

        await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<(TelegramUserSession Session, bool IsNew)> GetOrCreateSessionAsync(User from, CancellationToken ct)
    {
        var session = await _db.TelegramUserSessions
            .FirstOrDefaultAsync(s => s.TelegramUserId == from.Id, ct);

        if (session is null)
        {
            session = new TelegramUserSession
            {
                TelegramUserId = from.Id,
                FirstName = from.FirstName,
                UserName = from.Username,
                State = TelegramSessionState.Idle,
                Language = BotLocalizer.DefaultLanguage,
                LastInteractionAt = DateTime.UtcNow
            };
            _db.TelegramUserSessions.Add(session);
            return (session, true);
        }

        session.FirstName = from.FirstName;
        session.UserName = from.Username;
        return (session, false);
    }

    private async Task HandleCommandAsync(ITelegramBotClient bot, Message message, TelegramUserSession session, bool isNew, CancellationToken ct)
    {
        var raw = (message.Text ?? string.Empty).Trim();
        var atIdx = raw.IndexOf('@');
        var command = (atIdx > 0 ? raw[..atIdx] : raw).ToLowerInvariant();
        var lang = session.Language;
        var chatId = message.Chat.Id;

        switch (command)
        {
            case "/start":
                ResetDraft(session);
                if (isNew || session.Language == BotLocalizer.DefaultLanguage)
                {
                    await SendLanguagePickerAsync(bot, chatId, lang, ct);
                }
                else
                {
                    await bot.SendMessage(chatId, _localizer.Get(lang, "welcome"), cancellationToken: ct);
                }
                break;

            case "/language":
                await SendLanguagePickerAsync(bot, chatId, lang, ct);
                break;

            case "/report":
                ResetDraft(session);
                session.State = TelegramSessionState.AwaitingDescription;
                await bot.SendMessage(chatId, _localizer.Get(lang, "report_ask_description"), cancellationToken: ct);
                break;

            case "/status":
                await SendStatusAsync(bot, message, session, ct);
                break;

            case "/help":
                await bot.SendMessage(chatId, _localizer.Get(lang, "help"), cancellationToken: ct);
                break;

            case "/cancel":
                if (session.State == TelegramSessionState.Idle)
                {
                    await bot.SendMessage(chatId, _localizer.Get(lang, "info_use_report_to_start"), cancellationToken: ct);
                }
                else
                {
                    ResetDraft(session);
                    await bot.SendMessage(
                        chatId,
                        _localizer.Get(lang, "report_cancelled"),
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: ct);
                }
                break;

            case "/done":
                if (session.State == TelegramSessionState.AwaitingPhotos)
                {
                    if (session.DraftPhotoFileIds.Count == 0)
                    {
                        await bot.SendMessage(chatId, _localizer.Get(lang, "report_need_at_least_one_photo"), cancellationToken: ct);
                    }
                    else
                    {
                        session.State = TelegramSessionState.AwaitingLocation;
                        await AskForLocationAsync(bot, chatId, lang, ct);
                    }
                }
                else
                {
                    await bot.SendMessage(chatId, _localizer.Get(lang, "info_use_report_to_start"), cancellationToken: ct);
                }
                break;

            case "/confirm":
                if (session.State == TelegramSessionState.AwaitingConfirmation)
                {
                    await SubmitReportAsync(bot, message, session, ct);
                }
                else
                {
                    await bot.SendMessage(chatId, _localizer.Get(lang, "info_use_report_to_start"), cancellationToken: ct);
                }
                break;

            default:
                await bot.SendMessage(chatId, _localizer.Get(lang, "help"), cancellationToken: ct);
                break;
        }
    }

    private async Task HandleDescriptionAsync(ITelegramBotClient bot, Message message, TelegramUserSession session, CancellationToken ct)
    {
        var lang = session.Language;
        var chatId = message.Chat.Id;

        if (message.Photo is { Length: > 0 })
        {
            await bot.SendMessage(chatId, _localizer.Get(lang, "error_send_text_not_photo"), cancellationToken: ct);
            return;
        }

        var text = message.Text?.Trim() ?? string.Empty;
        if (text.Length < DescriptionMin)
        {
            await bot.SendMessage(chatId, _localizer.Get(lang, "report_description_too_short"), cancellationToken: ct);
            return;
        }
        if (text.Length > DescriptionMax)
        {
            await bot.SendMessage(chatId, _localizer.Get(lang, "report_description_too_long"), cancellationToken: ct);
            return;
        }

        session.DraftDescription = text;
        session.State = TelegramSessionState.AwaitingPhotos;
        await bot.SendMessage(chatId, _localizer.Get(lang, "report_ask_photos"), cancellationToken: ct);
    }

    private async Task HandlePhotosAsync(ITelegramBotClient bot, Message message, TelegramUserSession session, CancellationToken ct)
    {
        var lang = session.Language;
        var chatId = message.Chat.Id;

        if (message.Photo is null || message.Photo.Length == 0)
        {
            await bot.SendMessage(chatId, _localizer.Get(lang, "error_send_photo_not_text"), cancellationToken: ct);
            return;
        }

        if (session.DraftPhotoFileIds.Count >= MaxPhotos)
        {
            await bot.SendMessage(chatId, _localizer.Get(lang, "report_max_photos_reached"), cancellationToken: ct);
            return;
        }

        var largest = message.Photo.OrderByDescending(p => (long)p.Width * p.Height).First();
        var fileIds = session.DraftPhotoFileIds.ToList();
        fileIds.Add(largest.FileId);
        session.DraftPhotoFileIds = fileIds;

        await bot.SendMessage(chatId, _localizer.Get(lang, "report_photo_saved", fileIds.Count), cancellationToken: ct);
    }

    private async Task HandleLocationAsync(ITelegramBotClient bot, Message message, TelegramUserSession session, CancellationToken ct)
    {
        var lang = session.Language;
        var chatId = message.Chat.Id;

        if (message.Location is null)
        {
            await bot.SendMessage(chatId, _localizer.Get(lang, "error_send_location_not_text"), cancellationToken: ct);
            await AskForLocationAsync(bot, chatId, lang, ct);
            return;
        }

        session.DraftLatitude = message.Location.Latitude;
        session.DraftLongitude = message.Location.Longitude;
        session.State = TelegramSessionState.AwaitingConfirmation;

        var preview = session.DraftDescription ?? string.Empty;
        if (preview.Length > 100) preview = preview[..100] + "…";

        var lat = session.DraftLatitude!.Value.ToString("F5", CultureInfo.InvariantCulture);
        var lng = session.DraftLongitude!.Value.ToString("F5", CultureInfo.InvariantCulture);

        await bot.SendMessage(chatId, _localizer.Get(lang, "report_location_received"), replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        await bot.SendMessage(
            chatId,
            _localizer.Get(lang, "report_confirmation_summary",
                preview, session.DraftPhotoFileIds.Count, lat, lng),
            cancellationToken: ct);
    }

    private async Task HandleConfirmationAsync(ITelegramBotClient bot, Message message, TelegramUserSession session, CancellationToken ct)
    {
        await bot.SendMessage(
            message.Chat.Id,
            _localizer.Get(session.Language, "info_use_report_to_start"),
            cancellationToken: ct);
    }

    private async Task SendIdleHelpAsync(ITelegramBotClient bot, Message message, TelegramUserSession session, CancellationToken ct)
    {
        await bot.SendMessage(
            message.Chat.Id,
            _localizer.Get(session.Language, "info_use_report_to_start"),
            cancellationToken: ct);
    }

    private async Task SendLanguagePickerAsync(ITelegramBotClient bot, long chatId, string currentLang, CancellationToken ct)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(_localizer.Get(currentLang, "btn_lang_ru"), "lang:ru"),
                InlineKeyboardButton.WithCallbackData(_localizer.Get(currentLang, "btn_lang_ky"), "lang:ky"),
                InlineKeyboardButton.WithCallbackData(_localizer.Get(currentLang, "btn_lang_en"), "lang:en")
            }
        });

        await bot.SendMessage(
            chatId,
            _localizer.Get(currentLang, "language_picker_prompt"),
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    private async Task AskForLocationAsync(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { KeyboardButton.WithRequestLocation(_localizer.Get(lang, "btn_send_location")) }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await bot.SendMessage(
            chatId,
            _localizer.Get(lang, "report_ask_location"),
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    private async Task SendStatusAsync(ITelegramBotClient bot, Message message, TelegramUserSession session, CancellationToken ct)
    {
        var lang = session.Language;

        var reports = await _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.TelegramUserId == session.TelegramUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new { r.Id, r.Description, r.Status, r.CreatedAt })
            .ToListAsync(ct);

        if (reports.Count == 0)
        {
            await bot.SendMessage(message.Chat.Id, _localizer.Get(lang, "status_no_reports"), cancellationToken: ct);
            return;
        }

        var lines = new List<string> { _localizer.Get(lang, "status_header") };
        foreach (var r in reports)
        {
            var preview = r.Description.Length > 60 ? r.Description[..60] + "…" : r.Description;
            var statusName = _localizer.Get(lang, GetStatusKey(r.Status));
            lines.Add(_localizer.Get(lang, "status_line", r.Id.ToString()[..8], statusName, preview));
        }

        await bot.SendMessage(message.Chat.Id, string.Join('\n', lines), cancellationToken: ct);
    }

    private async Task SubmitReportAsync(ITelegramBotClient bot, Message message, TelegramUserSession session, CancellationToken ct)
    {
        var lang = session.Language;

        if (string.IsNullOrWhiteSpace(session.DraftDescription)
            || session.DraftPhotoFileIds.Count == 0
            || !session.DraftLatitude.HasValue
            || !session.DraftLongitude.HasValue)
        {
            await bot.SendMessage(message.Chat.Id, _localizer.Get(lang, "error_unknown_state"), cancellationToken: ct);
            return;
        }

        var savedPaths = new List<string>();
        foreach (var fileId in session.DraftPhotoFileIds)
        {
            try
            {
                savedPaths.Add(await DownloadPhotoAsync(bot, fileId, ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download photo {FileId} for Telegram user {UserId}",
                    fileId, session.TelegramUserId);
            }
        }

        if (savedPaths.Count == 0)
        {
            await bot.SendMessage(message.Chat.Id, _localizer.Get(lang, "error_unknown_state"), cancellationToken: ct);
            return;
        }

        var report = new DumpsiteReport
        {
            ReporterId = null,
            Description = session.DraftDescription!,
            Latitude = session.DraftLatitude!.Value,
            Longitude = session.DraftLongitude!.Value,
            Status = DumpsiteStatus.New,
            PhotoPaths = savedPaths,
            Source = ReportSource.Telegram,
            TelegramUserId = session.TelegramUserId,
            TelegramUserName = session.UserName ?? session.FirstName
        };
        _db.DumpsiteReports.Add(report);

        ResetDraft(session);

        await _db.SaveChangesAsync(ct);

        await bot.SendMessage(
            message.Chat.Id,
            _localizer.Get(lang, "report_submitted", report.Id.ToString()[..8]),
            cancellationToken: ct);

        _logger.LogInformation(
            "Telegram report {ReportId} submitted by Telegram user {UserId} with {PhotoCount} photo(s)",
            report.Id, session.TelegramUserId, savedPaths.Count);
    }

    private async Task<string> DownloadPhotoAsync(ITelegramBotClient bot, string fileId, CancellationToken ct)
    {
        var file = await bot.GetFile(fileId, ct);
        var ext = Path.GetExtension(file.FilePath ?? string.Empty);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";

        var fileName = $"{Guid.NewGuid()}{ext}";
        var folder = Path.Combine(_env.WebRootPath, "uploads", "dumpsites");
        Directory.CreateDirectory(folder);
        var savePath = Path.Combine(folder, fileName);

        await using var stream = File.Create(savePath);
        await bot.DownloadFile(file.FilePath!, stream, ct);

        return $"/uploads/dumpsites/{fileName}";
    }

    private static string GetStatusKey(DumpsiteStatus status) => status switch
    {
        DumpsiteStatus.New        => "status_dumpsite_new",
        DumpsiteStatus.InReview   => "status_dumpsite_in_review",
        DumpsiteStatus.Confirmed  => "status_dumpsite_confirmed",
        DumpsiteStatus.Rejected   => "status_dumpsite_rejected",
        DumpsiteStatus.Resolved   => "status_dumpsite_resolved",
        _                          => "status_dumpsite_new"
    };

    private static void ResetDraft(TelegramUserSession session)
    {
        session.State = TelegramSessionState.Idle;
        session.DraftDescription = null;
        session.DraftPhotoFileIds = new List<string>();
        session.DraftLatitude = null;
        session.DraftLongitude = null;
    }
}

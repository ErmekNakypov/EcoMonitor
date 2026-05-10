using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Web.Models.Admin.Telegram;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace EcoMonitor.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Administrator)]
[Route("Admin/Telegram")]
public class TelegramController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramController> _logger;

    public TelegramController(
        IApplicationDbContext db,
        IConfiguration configuration,
        ILogger<TelegramController> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var token = _configuration["Telegram:BotToken"];
        var configuredUsername = _configuration["Telegram:BotUsername"];

        var model = new TelegramStatusViewModel
        {
            IsConfigured = !string.IsNullOrWhiteSpace(token),
            BotUsername = configuredUsername
        };

        if (model.IsConfigured)
        {
            try
            {
                var bot = new TelegramBotClient(token!);
                var me = await bot.GetMe(ct);
                model.IsConnected = true;
                model.BotUsername = me.Username ?? configuredUsername;
                model.BotId = me.Id;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram GetMe failed in admin status page");
                model.IsConnected = false;
                model.ConnectionError = ex.Message;
            }
        }

        model.TotalTelegramReports = await _db.DumpsiteReports
            .AsNoTracking()
            .CountAsync(r => r.Source == ReportSource.Telegram, ct);

        var distinctIds = await _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.TelegramUserId != null)
            .Select(r => r.TelegramUserId!.Value)
            .Distinct()
            .ToListAsync(ct);
        model.UniqueTelegramUsers = distinctIds.Count;

        var sessions = await _db.TelegramUserSessions
            .AsNoTracking()
            .OrderByDescending(s => s.LastInteractionAt)
            .Take(10)
            .ToListAsync(ct);

        var sessionUserIds = sessions.Select(s => s.TelegramUserId).ToList();
        var reportCounts = await _db.DumpsiteReports
            .AsNoTracking()
            .Where(r => r.TelegramUserId != null && sessionUserIds.Contains(r.TelegramUserId!.Value))
            .GroupBy(r => r.TelegramUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var countByUser = reportCounts.ToDictionary(r => r.UserId, r => r.Count);

        model.RecentUsers = sessions
            .Select(s => new TelegramRecentUserViewModel
            {
                TelegramUserId = s.TelegramUserId,
                UserName = s.UserName,
                FirstName = s.FirstName,
                LastInteractionAt = s.LastInteractionAt,
                TotalReports = countByUser.GetValueOrDefault(s.TelegramUserId, 0)
            })
            .ToList();

        return View(model);
    }
}

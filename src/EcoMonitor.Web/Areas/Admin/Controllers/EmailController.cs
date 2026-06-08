using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Domain.Constants;
using EcoMonitor.Domain.Enums;
using EcoMonitor.Web.Models.Admin.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace EcoMonitor.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Administrator)]
[Route("Admin/Email")]
public class EmailController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly EmailOptions _options;
    private readonly IEmailQueue _queue;
    private readonly ILogger<EmailController> _logger;
    private readonly IStringLocalizer<EmailController> _localizer;

    public EmailController(
        IApplicationDbContext db,
        IOptions<EmailOptions> options,
        IEmailQueue queue,
        ILogger<EmailController> logger,
        IStringLocalizer<EmailController> localizer)
    {
        _db = db;
        _options = options.Value;
        _queue = queue;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var counts = await _db.EmailMessages
            .AsNoTracking()
            .Where(m => m.CreatedAt >= sevenDaysAgo)
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byStatus = counts.ToDictionary(c => c.Status, c => c.Count);
        var failedTotal = await _db.EmailMessages
            .AsNoTracking()
            .CountAsync(m => m.Status == EmailStatus.Failed, ct);

        var recent = await _db.EmailMessages
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .Select(m => new RecentEmailViewModel
            {
                Id = m.Id,
                Subject = m.Subject,
                ToAddress = m.ToAddress,
                Status = m.Status,
                AttemptCount = m.AttemptCount,
                TemplateName = m.TemplateName,
                CreatedAt = m.CreatedAt,
                SentAt = m.SentAt,
                NextAttemptAt = m.NextAttemptAt,
                LastError = m.LastError
            })
            .ToListAsync(ct);

        var model = new EmailDiagnosticsViewModel
        {
            IsConfigured = !string.IsNullOrWhiteSpace(_options.Host) && !string.IsNullOrWhiteSpace(_options.Username),
            Host = _options.Host,
            Port = _options.Port,
            UseSsl = _options.UseSsl,
            Username = _options.Username,
            MaskedPassword = string.IsNullOrEmpty(_options.Password) ? "(not set)" : "********",
            FromAddress = _options.FromAddress,
            FromName = _options.FromName,
            MaxRetryAttempts = _options.MaxRetryAttempts,
            RetryDelayMinutes = _options.RetryDelayMinutes,
            PendingCount7d = byStatus.GetValueOrDefault(EmailStatus.Pending, 0),
            SentCount7d = byStatus.GetValueOrDefault(EmailStatus.Sent, 0),
            FailedCount7d = byStatus.GetValueOrDefault(EmailStatus.Failed, 0),
            FailedTotal = failedTotal,
            Recent = recent
        };

        return View(model);
    }

    [HttpPost("SendTest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTest(string toAddress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toAddress) || !toAddress.Contains('@'))
        {
            TempData["ErrorMessage"] = _localizer["InvalidAddressError"].Value;
            return RedirectToAction(nameof(Index));
        }

        var html = """
            <!DOCTYPE html><html><body style="font-family:Arial;color:#1F2937;">
              <div style="max-width:520px;margin:24px auto;padding:24px;background:#fff;border:1px solid #E5E7EB;border-radius:12px;">
                <h2 style="color:#2D6A4F;margin-top:0;">EcoMonitor test email</h2>
                <p>This is a test message from the EcoMonitor email diagnostics page.</p>
                <p style="color:#6B7280;font-size:13px;">If you received this, SMTP is configured correctly.</p>
              </div>
            </body></html>
            """;

        try
        {
            await _queue.EnqueueAsync(toAddress.Trim(), string.Empty, "EcoMonitor test email", html, "TestEmail", null, ct);
            TempData["SuccessMessage"] = _localizer["QueuedSuccessFormat", toAddress].Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue test email to {To}", toAddress);
            TempData["ErrorMessage"] = _localizer["QueueErrorFormat", ex.Message].Value;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("RetryFailed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryFailed(CancellationToken ct)
    {
        var failed = await _db.EmailMessages
            .Where(m => m.Status == EmailStatus.Failed)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var msg in failed)
        {
            msg.Status = EmailStatus.Pending;
            msg.NextAttemptAt = now;
            msg.AttemptCount = 0;
            msg.LastError = null;
        }

        await _db.SaveChangesAsync(ct);

        TempData["SuccessMessage"] = failed.Count == 0
            ? _localizer["NoFailedToRetry"].Value
            : _localizer["RequeuedFormat", failed.Count].Value;

        return RedirectToAction(nameof(Index));
    }
}

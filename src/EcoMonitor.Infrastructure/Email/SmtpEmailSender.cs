using System.Net;
using System.Net.Mail;
using System.Text;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Application.Common.Models;
using EcoMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// SmtpClient is marked obsolete in net9 in favour of MailKit, but it remains the
// only built-in SMTP client and meets the project's "no extra dependencies" rule.
#pragma warning disable SYSLIB0014

namespace EcoMonitor.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> TrySendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.Username))
        {
            _logger.LogWarning("SMTP not configured. Email will not be sent: {Subject}", message.Subject);
            return false;
        }

        if (string.IsNullOrWhiteSpace(message.ToAddress))
        {
            _logger.LogWarning("No recipient address for email: {Subject}", message.Subject);
            return false;
        }

        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                Timeout = 30000
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            mail.To.Add(new MailAddress(message.ToAddress, message.ToName));

            if (!string.IsNullOrEmpty(message.TextBody))
            {
                var plainView = AlternateView.CreateAlternateViewFromString(
                    message.TextBody, Encoding.UTF8, "text/plain");
                mail.AlternateViews.Add(plainView);
            }

            await client.SendMailAsync(mail, ct);
            _logger.LogInformation("Email sent: {Subject} to {To}", message.Subject, message.ToAddress);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogWarning(
                ex,
                "SMTP error sending email {Subject} to {To}: {Code}",
                message.Subject, message.ToAddress, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email {Subject} to {To}", message.Subject, message.ToAddress);
            return false;
        }
    }
}

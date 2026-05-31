using EcoMonitor.Application.Common.Models;
using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.SubmitDumpsiteReport;

// ReporterId is nullable so the Telegram bot path (anonymous) can route through
// the same command. PreSavedPhotoPaths is for the Telegram path: those photos
// are already on disk under wwwroot/uploads/dumpsites/, so the handler skips its
// own save loop and uses the paths directly. Web callers leave the Telegram +
// pre-saved fields at their defaults.
public sealed record SubmitDumpsiteReportCommand(
    Guid? ReporterId,
    string Description,
    double Latitude,
    double Longitude,
    IReadOnlyList<UploadedPhotoDto> Photos,
    ReportSource Source = ReportSource.Web,
    long? TelegramUserId = null,
    string? TelegramUserName = null,
    IReadOnlyList<string>? PreSavedPhotoPaths = null
) : IRequest<Guid>;

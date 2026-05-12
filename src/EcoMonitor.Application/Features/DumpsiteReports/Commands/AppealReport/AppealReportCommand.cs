using EcoMonitor.Application.Common.Models;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.AppealReport;

public sealed record AppealReportCommand(
    Guid ReportId,
    Guid CitizenId,
    string AppealReason,
    IReadOnlyList<UploadedPhotoDto> Photos) : IRequest<Unit>;

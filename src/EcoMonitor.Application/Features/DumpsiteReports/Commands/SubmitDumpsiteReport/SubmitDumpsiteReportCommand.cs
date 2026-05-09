using EcoMonitor.Application.Common.Models;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.SubmitDumpsiteReport;

public sealed record SubmitDumpsiteReportCommand(
    Guid ReporterId,
    string Description,
    double Latitude,
    double Longitude,
    IReadOnlyList<UploadedPhotoDto> Photos
) : IRequest<Guid>;

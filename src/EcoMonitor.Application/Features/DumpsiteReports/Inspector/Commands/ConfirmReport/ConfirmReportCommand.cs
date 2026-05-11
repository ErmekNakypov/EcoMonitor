using EcoMonitor.Application.Common.Models;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ConfirmReport;

public sealed record ConfirmReportCommand(
    Guid ReportId,
    Guid InspectorId,
    string Observations,
    IReadOnlyList<UploadedPhotoDto> InspectionPhotos) : IRequest<Unit>;

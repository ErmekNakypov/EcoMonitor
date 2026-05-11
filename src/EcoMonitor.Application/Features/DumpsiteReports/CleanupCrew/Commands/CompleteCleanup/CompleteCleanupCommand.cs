using EcoMonitor.Application.Common.Models;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Commands.CompleteCleanup;

public sealed record CompleteCleanupCommand(
    Guid ReportId,
    Guid CleanupUserId,
    string Notes,
    IReadOnlyList<UploadedPhotoDto> AfterPhotos) : IRequest<Unit>;

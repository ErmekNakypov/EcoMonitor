using EcoMonitor.Application.Common.Models;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Commands.StartCleanup;

public sealed record StartCleanupCommand(
    Guid ReportId,
    Guid CleanupUserId,
    IReadOnlyList<UploadedPhotoDto> BeforePhotos) : IRequest<Unit>;

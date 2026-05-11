using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Commands.TakeForCleanup;

public sealed record TakeForCleanupCommand(Guid ReportId, Guid CleanupUserId) : IRequest<Unit>;

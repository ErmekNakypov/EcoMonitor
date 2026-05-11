using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.RejectCleanup;

public sealed record RejectCleanupCommand(Guid ReportId, Guid InspectorId, string Reason) : IRequest<Unit>;

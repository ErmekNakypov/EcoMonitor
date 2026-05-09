using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ResolveReport;

public sealed record ResolveReportCommand(Guid ReportId, Guid InspectorId, string Notes) : IRequest<Unit>;

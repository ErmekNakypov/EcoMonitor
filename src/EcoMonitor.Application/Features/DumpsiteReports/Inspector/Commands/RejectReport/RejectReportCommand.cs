using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.RejectReport;

public sealed record RejectReportCommand(Guid ReportId, Guid InspectorId, string Reason) : IRequest<Unit>;

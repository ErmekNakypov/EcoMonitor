using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ConfirmReport;

public sealed record ConfirmReportCommand(Guid ReportId, Guid InspectorId) : IRequest<Unit>;

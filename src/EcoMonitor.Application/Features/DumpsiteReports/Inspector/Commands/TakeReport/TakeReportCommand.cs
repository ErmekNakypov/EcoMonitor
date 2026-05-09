using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.TakeReport;

public sealed record TakeReportCommand(Guid ReportId, Guid InspectorId) : IRequest<Unit>;

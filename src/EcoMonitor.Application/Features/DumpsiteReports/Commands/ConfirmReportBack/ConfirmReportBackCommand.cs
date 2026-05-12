using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.ConfirmReportBack;

public sealed record ConfirmReportBackCommand(
    Guid ReportId,
    Guid InspectorId,
    string DecisionNotes) : IRequest<Unit>;

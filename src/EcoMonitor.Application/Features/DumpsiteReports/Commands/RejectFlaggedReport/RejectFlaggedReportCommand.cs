using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.RejectFlaggedReport;

public sealed record RejectFlaggedReportCommand(
    Guid ReportId,
    Guid InspectorId,
    string DecisionNotes) : IRequest<Unit>;

using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.ReassignToAnotherCrew;

public sealed record ReassignToAnotherCrewCommand(
    Guid ReportId,
    Guid InspectorId,
    string DecisionNotes) : IRequest<Unit>;

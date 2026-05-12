using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.DismissAppeal;

public sealed record DismissAppealCommand(
    Guid ReportId,
    Guid InspectorId,
    string ResolutionNotes) : IRequest<Unit>;

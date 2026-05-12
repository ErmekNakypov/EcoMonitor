using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.UpholdAppeal;

public sealed record UpholdAppealCommand(
    Guid ReportId,
    Guid InspectorId,
    string ResolutionNotes) : IRequest<Unit>;

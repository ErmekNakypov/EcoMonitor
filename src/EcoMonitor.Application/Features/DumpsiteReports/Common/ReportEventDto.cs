using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Application.Features.DumpsiteReports.Common;

public sealed record ReportEventDto(
    DumpsiteEventType EventType,
    DateTime OccurredAt,
    string ActorRole,
    string ActorDisplayName,
    string? Notes);

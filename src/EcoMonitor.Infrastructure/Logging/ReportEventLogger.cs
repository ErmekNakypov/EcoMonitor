using System.Text.Json;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;

namespace EcoMonitor.Infrastructure.Logging;

public sealed class ReportEventLogger : IReportEventLogger
{
    private static readonly JsonSerializerOptions PayloadOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IApplicationDbContext _db;

    public ReportEventLogger(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        Guid reportId,
        DumpsiteEventType eventType,
        Guid? actorUserId,
        string actorRole,
        string actorDisplayName,
        string? notes = null,
        object? payload = null,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var evt = new DumpsiteReportEvent
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            EventType = eventType,
            OccurredAt = now,
            ActorUserId = actorUserId,
            ActorRole = string.IsNullOrWhiteSpace(actorRole) ? "System" : actorRole,
            ActorDisplayName = string.IsNullOrWhiteSpace(actorDisplayName) ? "System" : actorDisplayName,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            PayloadJson = payload is null ? null : JsonSerializer.Serialize(payload, PayloadOpts),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.DumpsiteReportEvents.Add(evt);
        await _db.SaveChangesAsync(ct);
    }
}

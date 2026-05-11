using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.RejectCleanup;

public class RejectCleanupHandler : IRequestHandler<RejectCleanupCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<RejectCleanupHandler> _logger;

    public RejectCleanupHandler(IApplicationDbContext dbContext, ILogger<RejectCleanupHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(RejectCleanupCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
        {
            throw new DomainException("A rework reason of at least 10 characters is required.");
        }

        var report = await _dbContext.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.Status != DumpsiteStatus.AwaitingVerification)
        {
            throw new DomainException("Only reports awaiting verification can be sent back for rework.");
        }

        report.Status = DumpsiteStatus.CleanupInProgress;
        // Stash the feedback in CleanupNotes so the crew sees it on the details page.
        // Append rather than overwrite to preserve the original cleanup notes.
        var feedback = $"[Rework requested {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC] {request.Reason.Trim()}";
        report.CleanupNotes = string.IsNullOrWhiteSpace(report.CleanupNotes)
            ? feedback
            : report.CleanupNotes + "\n\n" + feedback;
        report.CleanupCompletedAt = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Inspector {InspectorId} sent report {ReportId} back for cleanup rework",
            request.InspectorId, report.Id);

        return Unit.Value;
    }
}

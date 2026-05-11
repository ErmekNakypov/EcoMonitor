using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.CleanupCrew.Commands.TakeForCleanup;

public class TakeForCleanupHandler : IRequestHandler<TakeForCleanupCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<TakeForCleanupHandler> _logger;

    public TakeForCleanupHandler(IApplicationDbContext dbContext, ILogger<TakeForCleanupHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(TakeForCleanupCommand request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.Status != DumpsiteStatus.Confirmed)
        {
            throw new DomainException("Only confirmed reports can be taken for cleanup.");
        }

        if (report.CleanupCrewId is not null && report.CleanupCrewId != request.CleanupUserId)
        {
            throw new DomainException("This report is already assigned to another cleanup crew member.");
        }

        report.CleanupCrewId = request.CleanupUserId;
        // Status stays Confirmed until the crew actually starts cleanup.
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cleanup user {UserId} took report {ReportId}",
            request.CleanupUserId, report.Id);

        return Unit.Value;
    }
}

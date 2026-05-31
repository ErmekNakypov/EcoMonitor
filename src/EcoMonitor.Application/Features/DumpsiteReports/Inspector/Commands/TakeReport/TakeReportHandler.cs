using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.TakeReport;

public class TakeReportHandler : IRequestHandler<TakeReportCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserLookupService _userLookup;
    private readonly IReportEventLogger _events;
    private readonly ILogger<TakeReportHandler> _logger;

    public TakeReportHandler(
        IApplicationDbContext dbContext,
        IUserLookupService userLookup,
        IReportEventLogger events,
        ILogger<TakeReportHandler> logger)
    {
        _dbContext = dbContext;
        _userLookup = userLookup;
        _events = events;
        _logger = logger;
    }

    public async Task<Unit> Handle(TakeReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        // Two claimable shapes:
        //   - Status=New, unassigned    → bot-submitted, advances to InReview.
        //   - Status=InReview, unassigned → web report whose district had no
        //                                    inspector, claimed without status change.
        // Either way the report ends up Status=InReview, AssignedInspectorId=me.
        var isClaimableNew = report.Status == DumpsiteStatus.New && report.AssignedInspectorId is null;
        var isClaimableUnassignedReview = report.Status == DumpsiteStatus.InReview && report.AssignedInspectorId is null;
        if (!isClaimableNew && !isClaimableUnassignedReview)
        {
            throw new DomainException("This report is already taken or not in a claimable state.");
        }

        report.Status = DumpsiteStatus.InReview;
        report.AssignedInspectorId = request.InspectorId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Inspector {InspectorId} took report {ReportId}", request.InspectorId, report.Id);

        var actor = await _userLookup.GetByIdAsync(request.InspectorId, cancellationToken);
        await _events.LogAsync(report.Id, DumpsiteEventType.InspectorTook,
            request.InspectorId, "Inspector", actor?.FullName ?? "Inspector",
            ct: cancellationToken);

        return Unit.Value;
    }
}

using EcoMonitor.Application.Common.Exceptions;
using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ConfirmReport;

public class ConfirmReportHandler : IRequestHandler<ConfirmReportCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ConfirmReportHandler> _logger;

    public ConfirmReportHandler(IApplicationDbContext dbContext, ILogger<ConfirmReportHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(ConfirmReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.DumpsiteReports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Report {request.ReportId} not found.");
        }

        if (report.AssignedInspectorId != request.InspectorId)
        {
            throw new ForbiddenException("You are not the assigned inspector for this report.");
        }

        if (report.Status != DumpsiteStatus.InReview)
        {
            throw new DomainException("Only reports in review can be confirmed.");
        }

        report.Status = DumpsiteStatus.Confirmed;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Inspector {InspectorId} confirmed report {ReportId}", request.InspectorId, report.Id);
        return Unit.Value;
    }
}

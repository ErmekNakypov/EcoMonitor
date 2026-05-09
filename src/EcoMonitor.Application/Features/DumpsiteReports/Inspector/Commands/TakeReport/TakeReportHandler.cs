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
    private readonly ILogger<TakeReportHandler> _logger;

    public TakeReportHandler(IApplicationDbContext dbContext, ILogger<TakeReportHandler> logger)
    {
        _dbContext = dbContext;
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

        if (report.Status != DumpsiteStatus.New || report.AssignedInspectorId is not null)
        {
            throw new DomainException("This report is already taken or not in New status.");
        }

        report.Status = DumpsiteStatus.InReview;
        report.AssignedInspectorId = request.InspectorId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Inspector {InspectorId} took report {ReportId}", request.InspectorId, report.Id);
        return Unit.Value;
    }
}

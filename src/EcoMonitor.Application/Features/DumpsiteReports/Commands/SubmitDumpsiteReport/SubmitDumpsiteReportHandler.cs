using EcoMonitor.Application.Common.Interfaces;
using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.SubmitDumpsiteReport;

public class SubmitDumpsiteReportHandler : IRequestHandler<SubmitDumpsiteReportCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<SubmitDumpsiteReportHandler> _logger;

    public SubmitDumpsiteReportHandler(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorage,
        ILogger<SubmitDumpsiteReportHandler> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Guid> Handle(SubmitDumpsiteReportCommand request, CancellationToken cancellationToken)
    {
        var savedPaths = new List<string>(request.Photos.Count);
        foreach (var photo in request.Photos)
        {
            var path = await _fileStorage.SaveAsync(photo, "dumpsites", cancellationToken);
            savedPaths.Add(path);
        }

        var report = new DumpsiteReport
        {
            ReporterId = request.ReporterId,
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Status = DumpsiteStatus.New,
            PhotoPaths = savedPaths
        };

        _dbContext.DumpsiteReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Dumpsite report {ReportId} submitted by {ReporterId} with {PhotoCount} photo(s)",
            report.Id, request.ReporterId, savedPaths.Count);

        return report.Id;
    }
}

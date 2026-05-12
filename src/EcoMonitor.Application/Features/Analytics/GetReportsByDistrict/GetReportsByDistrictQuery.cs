using MediatR;

namespace EcoMonitor.Application.Features.Analytics.GetReportsByDistrict;

public sealed record GetReportsByDistrictQuery() : IRequest<ReportsByDistrictResult>;

public sealed record ReportsByDistrictResult(IReadOnlyList<DistrictStat> Stats);

public sealed record DistrictStat(
    string Code,
    string NameRu,
    string ColorHex,
    int Total,
    int Resolved,
    int Rejected,
    int InProgress);

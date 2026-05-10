using EcoMonitor.Domain.Enums;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Public;

public sealed record GetPublicDumpsitesQuery() : IRequest<IReadOnlyList<PublicDumpsiteDto>>;

public sealed record PublicDumpsiteDto(
    Guid Id,
    string ShortDescription,
    DumpsiteStatus Status,
    double Latitude,
    double Longitude,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    int PhotoCount);

using EcoMonitor.Application.Common.Models;
using MediatR;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.FlagCleanup;

public sealed record FlagCleanupCommand(
    Guid ReportId,
    Guid CleanupCrewId,
    string Reason,
    string? AdditionalNotes,
    IReadOnlyList<UploadedPhotoDto> Photos) : IRequest<Unit>;

// Reason values are frozen — they live in dumpsite_reports.cleanup_rejection_reason
// and the UI dropdown / display mapper depends on these exact strings.
public static class FlagCleanupReasons
{
    public const string NoDumpsiteFound = "NoDumpsiteFound";
    public const string AlreadyClean = "AlreadyClean";
    public const string OutsideJurisdiction = "OutsideJurisdiction";
    public const string PhotoMismatch = "PhotoMismatch";
    public const string Other = "Other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        NoDumpsiteFound, AlreadyClean, OutsideJurisdiction, PhotoMismatch, Other
    };

    public static string Display(string? reason) => reason switch
    {
        NoDumpsiteFound      => "No dumpsite found at this location",
        AlreadyClean         => "Site already clean",
        OutsideJurisdiction  => "Outside our jurisdiction",
        PhotoMismatch        => "Photo does not match the location",
        Other                => "Other",
        _                    => reason ?? "(unspecified)"
    };
}

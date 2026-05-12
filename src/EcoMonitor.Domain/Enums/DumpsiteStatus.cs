using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Domain.Enums;

// Numeric values frozen to preserve existing rows in dumpsite_reports.
// Append new states at the end; do not reorder.
public enum DumpsiteStatus
{
    [Display(Name = "New")]
    New = 0,

    [Display(Name = "In review")]
    InReview = 1,

    [Display(Name = "Confirmed")]
    Confirmed = 2,

    [Display(Name = "Resolved")]
    Resolved = 3,

    [Display(Name = "Rejected")]
    Rejected = 4,

    [Display(Name = "Cleanup in progress")]
    CleanupInProgress = 5,

    [Display(Name = "Awaiting verification")]
    AwaitingVerification = 6,

    [Display(Name = "Appealed")]
    Appealed = 7,

    [Display(Name = "Closed")]
    Closed = 8,

    [Display(Name = "Flagged by cleanup crew")]
    FlaggedByCleanupCrew = 9
}

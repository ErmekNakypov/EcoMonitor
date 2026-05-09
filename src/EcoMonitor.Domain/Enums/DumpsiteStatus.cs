using System.ComponentModel.DataAnnotations;

namespace EcoMonitor.Domain.Enums;

public enum DumpsiteStatus
{
    [Display(Name = "New")]
    New,

    [Display(Name = "In review")]
    InReview,

    [Display(Name = "Confirmed")]
    Confirmed,

    [Display(Name = "Resolved")]
    Resolved,

    [Display(Name = "Rejected")]
    Rejected
}

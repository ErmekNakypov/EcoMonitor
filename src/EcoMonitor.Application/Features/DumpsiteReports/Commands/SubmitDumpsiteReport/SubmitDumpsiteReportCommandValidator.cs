using EcoMonitor.Domain.Enums;
using FluentValidation;

namespace EcoMonitor.Application.Features.DumpsiteReports.Commands.SubmitDumpsiteReport;

public class SubmitDumpsiteReportCommandValidator : AbstractValidator<SubmitDumpsiteReportCommand>
{
    private const long MaxPhotoBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public SubmitDumpsiteReportCommandValidator()
    {
        RuleFor(c => c.Description)
            .NotEmpty()
            .Length(10, 1000);

        RuleFor(c => c.Latitude)
            .InclusiveBetween(-90.0, 90.0);

        RuleFor(c => c.Longitude)
            .InclusiveBetween(-180.0, 180.0);

        // Photos may be supplied either inline (Web) or as paths to files already
        // on disk (Telegram bot — see PreSavedPhotoPaths). Require at least one
        // of the two and cap the inline list at 5.
        RuleFor(c => c.Photos)
            .NotNull();

        RuleFor(c => c.Photos.Count)
            .LessThanOrEqualTo(5)
            .WithMessage("You can attach at most 5 photos.");

        // Photo presence: Web + Telegram citizen reports require at least one
        // photo. IoT-sourced reports (auto-created when a sensor reports a
        // full container) legitimately have no photo — they're system events,
        // not citizen observations. Exempt Source == Iot from this rule.
        RuleFor(c => c)
            .Must(c => c.Source == ReportSource.Iot
                    || (c.Photos != null && c.Photos.Count > 0)
                    || (c.PreSavedPhotoPaths != null && c.PreSavedPhotoPaths.Count > 0))
            .WithMessage("At least one photo is required.");

        RuleForEach(c => c.Photos).ChildRules(photo =>
        {
            photo.RuleFor(p => p.ContentType)
                .Must(ct => AllowedContentTypes.Contains(ct))
                .WithMessage("Photos must be JPEG, PNG, or WEBP.");

            photo.RuleFor(p => p.Content.Length)
                .LessThanOrEqualTo((int)MaxPhotoBytes)
                .WithMessage("Each photo must be 5 MB or smaller.");
        });
    }
}

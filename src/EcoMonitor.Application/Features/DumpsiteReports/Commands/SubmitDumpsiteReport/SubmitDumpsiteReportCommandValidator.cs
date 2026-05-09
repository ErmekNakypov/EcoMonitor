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

        RuleFor(c => c.Photos)
            .NotNull()
            .Must(p => p.Count <= 5)
            .WithMessage("You can attach at most 5 photos.");

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

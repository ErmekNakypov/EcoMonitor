using FluentValidation;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.ResolveReport;

public class ResolveReportCommandValidator : AbstractValidator<ResolveReportCommand>
{
    public ResolveReportCommandValidator()
    {
        RuleFor(c => c.Notes)
            .NotEmpty()
            .Length(10, 1000);
    }
}

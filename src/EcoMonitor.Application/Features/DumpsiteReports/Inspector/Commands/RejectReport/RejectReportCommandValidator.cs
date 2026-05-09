using FluentValidation;

namespace EcoMonitor.Application.Features.DumpsiteReports.Inspector.Commands.RejectReport;

public class RejectReportCommandValidator : AbstractValidator<RejectReportCommand>
{
    public RejectReportCommandValidator()
    {
        RuleFor(c => c.Reason)
            .NotEmpty()
            .Length(10, 1000);
    }
}

using EcoMonitor.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.WasteContainers.Commands.UpdateWasteContainer;

public class UpdateWasteContainerCommandValidator : AbstractValidator<UpdateWasteContainerCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateWasteContainerCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(c => c.Code)
            .NotEmpty()
            .Length(1, 50)
            .MustAsync(BeUniqueExcludingSelf)
            .WithMessage("A container with this code already exists.");

        RuleFor(c => c.Address)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(c => c.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(c => c.Longitude).InclusiveBetween(-180.0, 180.0);

        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.Status).IsInEnum();

        RuleFor(c => c.Capacity)
            .GreaterThan(0)
            .LessThanOrEqualTo(10000);

        RuleFor(c => c.InstalledAt)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("Installation date cannot be in the future.");
    }

    private async Task<bool> BeUniqueExcludingSelf(UpdateWasteContainerCommand cmd, string code, CancellationToken ct)
    {
        return !await _dbContext.WasteContainers.AnyAsync(c => c.Code == code && c.Id != cmd.Id, ct);
    }
}

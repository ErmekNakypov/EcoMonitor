using EcoMonitor.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Application.Features.WasteContainers.Commands.CreateWasteContainer;

public class CreateWasteContainerCommandValidator : AbstractValidator<CreateWasteContainerCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateWasteContainerCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(c => c.Code)
            .NotEmpty()
            .Length(1, 50)
            .MustAsync(BeUniqueCode)
            .WithMessage("A container with this code already exists.");

        RuleFor(c => c.Address)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(c => c.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(c => c.Longitude).InclusiveBetween(-180.0, 180.0);

        RuleFor(c => c.Type).IsInEnum();

        RuleFor(c => c.Capacity)
            .GreaterThan(0)
            .LessThanOrEqualTo(10000);

        RuleFor(c => c.InstalledAt)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("Installation date cannot be in the future.");
    }

    private async Task<bool> BeUniqueCode(string code, CancellationToken ct)
    {
        return !await _dbContext.WasteContainers.AnyAsync(c => c.Code == code, ct);
    }
}

using FluentValidation;
using Shared.Models;

namespace Shared.Validation;

// Service and ServiceCategory validators removed - validation is now handled in the Services domain
// Domain entities use their own business rules and validation through domain methods

public class BaseEntityValidator : AbstractValidator<BaseEntity>
{
    public BaseEntityValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Entity ID is required")
            .Must(BeValidGuid).WithMessage("Entity ID must be a valid GUID")
            .When(x => !string.IsNullOrEmpty(x.Id));

        RuleFor(x => x.CreatedAt)
            .NotEmpty().WithMessage("Created date is required");

        RuleFor(x => x.UpdatedAt)
            .NotEmpty().WithMessage("Updated date is required")
            .GreaterThanOrEqualTo(x => x.CreatedAt).WithMessage("Updated date must be greater than or equal to created date");
    }

    private static bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}
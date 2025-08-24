using FluentValidation;
using InternationalCenter.Shared.Models;

namespace InternationalCenter.Shared.Validators;

public class ContactValidator : AbstractValidator<Contact>
{
    public ContactValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(255)
            .WithMessage("Name must not exceed 255 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .WithMessage("Phone must not exceed 20 characters");

        RuleFor(x => x.Subject)
            .MaximumLength(255)
            .WithMessage("Subject must not exceed 255 characters");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required");

        RuleFor(x => x.Status)
            .Must(status => EntityValidationRules.ValidContactStatuses.Contains(status))
            .WithMessage($"Status must be one of: {string.Join(", ", EntityValidationRules.ValidContactStatuses)}");

        RuleFor(x => x.Type)
            .MaximumLength(100)
            .WithMessage("Type must not exceed 100 characters");

        RuleFor(x => x.ConsentGiven)
            .Equal(true)
            .WithMessage("Consent must be given for data processing");
    }
}
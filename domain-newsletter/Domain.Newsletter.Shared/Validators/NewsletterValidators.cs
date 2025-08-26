using FluentValidation;
// using Shared.Models; // Newsletter entity on hold

namespace Shared.Validators;

// Newsletter API is on hold - commenting out to allow Services API compilation
/*
public class NewsletterSubscriptionValidator : AbstractValidator<NewsletterSubscription>
{
    public NewsletterSubscriptionValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Name)
            .MaximumLength(255)
            .WithMessage("Name must not exceed 255 characters");

        RuleFor(x => x.Status)
            .Must(status => new[] { NewsletterStatus.Pending, NewsletterStatus.Confirmed, NewsletterStatus.Unsubscribed, NewsletterStatus.Bounced }.Contains(status))
            .WithMessage("Invalid newsletter status");

        RuleFor(x => x.ConsentGiven)
            .Equal(true)
            .WithMessage("Consent must be given for newsletter subscription");
    }
}
*/
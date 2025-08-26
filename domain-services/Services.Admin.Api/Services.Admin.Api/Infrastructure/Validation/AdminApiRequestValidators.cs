using FluentValidation;
using InternationalCenter.Services.Admin.Api.Infrastructure.Extensions;

namespace InternationalCenter.Services.Admin.Api.Infrastructure.Validation;

/// <summary>
/// FluentValidation validators for Admin API REST endpoint request DTOs
/// Implements medical-grade validation with comprehensive business rules and security constraints
/// Supports audit compliance and provides structured error responses
/// </summary>

/// <summary>
/// Validator for CreateServiceApiRequest with comprehensive business rules validation
/// Ensures medical-grade compliance and data integrity for service creation requests
/// </summary>
public class CreateServiceApiRequestValidator : AbstractValidator<CreateServiceApiRequest>
{
    public CreateServiceApiRequestValidator()
    {
        // BUSINESS RULE: Service title is mandatory and must meet quality standards
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Service title is required for medical-grade compliance")
            .Length(3, 200)
            .WithMessage("Service title must be between 3 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-\.\_\(\)]+$")
            .WithMessage("Service title can only contain letters, numbers, spaces, and basic punctuation (- . _ ( ))");

        // BUSINESS RULE: Service slug must be unique and URL-safe
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Service slug is required for API routing")
            .Length(3, 100)
            .WithMessage("Service slug must be between 3 and 100 characters")
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("Service slug must be lowercase letters, numbers, and hyphens only (URL-safe format)");

        // BUSINESS RULE: Service description is mandatory for user understanding
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Service description is required for user clarity")
            .Length(10, 500)
            .WithMessage("Service description must be between 10 and 500 characters")
            .Must(NotContainDangerousContent)
            .WithMessage("Service description contains potentially dangerous content");

        // BUSINESS RULE: Detailed description provides comprehensive information
        RuleFor(x => x.DetailedDescription)
            .Length(0, 2000)
            .WithMessage("Detailed description cannot exceed 2000 characters")
            .Must(NotContainDangerousContent)
            .WithMessage("Detailed description contains potentially dangerous content")
            .When(x => !string.IsNullOrEmpty(x.DetailedDescription));

        // BUSINESS RULE: Technologies array must have reasonable limits
        RuleFor(x => x.Technologies)
            .Must(x => x == null || x.Length <= 20)
            .WithMessage("Technologies list cannot exceed 20 items")
            .Must(x => x == null || x.All(tech => !string.IsNullOrWhiteSpace(tech) && tech.Length <= 100))
            .WithMessage("Each technology must be 1-100 characters and not empty")
            .When(x => x.Technologies != null);

        // BUSINESS RULE: Features array must have reasonable limits
        RuleFor(x => x.Features)
            .Must(x => x == null || x.Length <= 15)
            .WithMessage("Features list cannot exceed 15 items")
            .Must(x => x == null || x.All(feature => !string.IsNullOrWhiteSpace(feature) && feature.Length <= 200))
            .WithMessage("Each feature must be 1-200 characters and not empty")
            .When(x => x.Features != null);

        // BUSINESS RULE: Delivery modes must be valid options
        RuleFor(x => x.DeliveryModes)
            .Must(x => x == null || x.Length <= 10)
            .WithMessage("Delivery modes list cannot exceed 10 items")
            .Must(x => x == null || x.All(mode => IsValidDeliveryMode(mode)))
            .WithMessage("Each delivery mode must be a valid option: Digital, Physical, Hybrid, Cloud, On-Premise")
            .When(x => x.DeliveryModes != null);

        // BUSINESS RULE: Icon must be reasonable size if provided
        RuleFor(x => x.Icon)
            .Length(0, 200)
            .WithMessage("Icon reference cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Icon));

        // BUSINESS RULE: Image must be reasonable size if provided
        RuleFor(x => x.Image)
            .Length(0, 500)
            .WithMessage("Image reference cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Image));

        // AUDIT COMPLIANCE: RequestId is mandatory for medical-grade audit trail
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("RequestId is required for medical-grade audit compliance")
            .Must(BeValidGuid)
            .WithMessage("RequestId must be a valid GUID for audit trail integrity");
    }

    private static bool NotContainDangerousContent(string content)
    {
        if (string.IsNullOrEmpty(content)) return true;
        
        // Security check for potentially dangerous content
        var dangerousPatterns = new[]
        {
            "<script", "javascript:", "vbscript:", "onload=", "onerror=",
            "eval(", "document.cookie", "window.location", "innerHTML"
        };
        
        return !dangerousPatterns.Any(pattern => 
            content.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValidDeliveryMode(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode)) return false;
        
        var validModes = new[] { "Digital", "Physical", "Hybrid", "Cloud", "On-Premise" };
        return validModes.Contains(mode, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}

/// <summary>
/// Validator for UpdateServiceApiRequest with business rules for service updates
/// Ensures data integrity and audit compliance for service modification requests
/// </summary>
public class UpdateServiceApiRequestValidator : AbstractValidator<UpdateServiceApiRequest>
{
    public UpdateServiceApiRequestValidator()
    {
        // BUSINESS RULE: Title updates must meet quality standards
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Service title is required for updates")
            .Length(3, 200)
            .WithMessage("Service title must be between 3 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-\.\_\(\)]+$")
            .WithMessage("Service title can only contain letters, numbers, spaces, and basic punctuation")
            .When(x => !string.IsNullOrEmpty(x.Title));

        // BUSINESS RULE: Description updates must be meaningful
        RuleFor(x => x.Description)
            .Length(10, 500)
            .WithMessage("Service description must be between 10 and 500 characters")
            .Must(NotContainDangerousContent)
            .WithMessage("Service description contains potentially dangerous content")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // BUSINESS RULE: Detailed description updates have size limits
        RuleFor(x => x.DetailedDescription)
            .Length(0, 2000)
            .WithMessage("Detailed description cannot exceed 2000 characters")
            .Must(NotContainDangerousContent)
            .WithMessage("Detailed description contains potentially dangerous content")
            .When(x => !string.IsNullOrEmpty(x.DetailedDescription));

        // BUSINESS RULE: Technology updates must be reasonable
        RuleFor(x => x.Technologies)
            .Must(x => x == null || x.Length <= 20)
            .WithMessage("Technologies list cannot exceed 20 items")
            .Must(x => x == null || x.All(tech => !string.IsNullOrWhiteSpace(tech) && tech.Length <= 100))
            .WithMessage("Each technology must be 1-100 characters and not empty")
            .When(x => x.Technologies != null);

        // AUDIT COMPLIANCE: RequestId is mandatory for medical-grade audit trail
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("RequestId is required for medical-grade audit compliance")
            .Must(BeValidGuid)
            .WithMessage("RequestId must be a valid GUID for audit trail integrity");
    }

    private static bool NotContainDangerousContent(string content)
    {
        if (string.IsNullOrEmpty(content)) return true;
        
        var dangerousPatterns = new[]
        {
            "<script", "javascript:", "vbscript:", "onload=", "onerror=",
            "eval(", "document.cookie", "window.location", "innerHTML"
        };
        
        return !dangerousPatterns.Any(pattern => 
            content.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}

/// <summary>
/// Validator for PublishServiceApiRequest with business rules for service publishing
/// Ensures audit compliance for service state changes
/// </summary>
public class PublishServiceApiRequestValidator : AbstractValidator<PublishServiceApiRequest>
{
    public PublishServiceApiRequestValidator()
    {
        // AUDIT COMPLIANCE: RequestId is mandatory for medical-grade audit trail
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("RequestId is required for medical-grade audit compliance")
            .Must(BeValidGuid)
            .WithMessage("RequestId must be a valid GUID for audit trail integrity");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}

/// <summary>
/// Validator for DeleteServiceApiRequest with business rules for service deletion
/// Ensures audit compliance for destructive operations
/// </summary>
public class DeleteServiceApiRequestValidator : AbstractValidator<DeleteServiceApiRequest>
{
    public DeleteServiceApiRequestValidator()
    {
        // AUDIT COMPLIANCE: RequestId is mandatory for medical-grade audit trail
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("RequestId is required for medical-grade audit compliance")
            .Must(BeValidGuid)
            .WithMessage("RequestId must be a valid GUID for audit trail integrity");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
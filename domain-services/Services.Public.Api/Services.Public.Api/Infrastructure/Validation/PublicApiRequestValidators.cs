using FluentValidation;
using InternationalCenter.Services.Public.Api.Application.UseCases;

namespace InternationalCenter.Services.Public.Api.Infrastructure.Validation;

/// <summary>
/// FluentValidation validators for Services Public API request DTOs
/// Implements security-focused validation for public-facing endpoints with anonymous access
/// Ensures query parameter safety and prevents abuse while maintaining performance
/// </summary>

/// <summary>
/// Validator for ServicesQueryRequest with comprehensive query parameter validation
/// Focuses on preventing abuse and ensuring reasonable query limits for public access
/// </summary>
public class ServicesQueryRequestValidator : AbstractValidator<ServicesQueryRequest>
{
    public ServicesQueryRequestValidator()
    {
        // SECURITY RULE: Pagination must be within reasonable bounds to prevent abuse
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("Page number cannot exceed 10,000 to prevent resource abuse");

        // SECURITY RULE: Page size must be reasonable to prevent DoS attacks
        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100 items to prevent resource exhaustion");

        // SECURITY RULE: Category filter must be safe and reasonable
        RuleFor(x => x.Category)
            .Length(1, 100)
            .WithMessage("Category filter must be 1-100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-\_]+$")
            .WithMessage("Category filter can only contain letters, numbers, spaces, hyphens, and underscores")
            .Must(NotContainSqlInjectionPatterns)
            .WithMessage("Category filter contains potentially dangerous content")
            .When(x => !string.IsNullOrEmpty(x.Category));

        // SECURITY RULE: Search term must be safe and not too broad
        RuleFor(x => x.SearchTerm)
            .Length(1, 200)
            .WithMessage("Search term must be 1-200 characters")
            .Must(NotContainSqlInjectionPatterns)
            .WithMessage("Search term contains potentially dangerous content")
            .Must(NotContainWildcardAbuse)
            .WithMessage("Search term contains excessive wildcards that could impact performance")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));

        // BUSINESS RULE: Sort criteria must be from allowed values
        RuleFor(x => x.SortBy)
            .Must(BeValidSortOption)
            .WithMessage("Sort option must be one of: title, date, priority, featured, category")
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        // AUDIT COMPLIANCE: Request ID should be valid if provided
        RuleFor(x => x.RequestId)
            .Must(BeValidGuid)
            .WithMessage("RequestId must be a valid GUID")
            .When(x => !string.IsNullOrEmpty(x.RequestId));

        // SECURITY RULE: User agent should not be excessively long
        RuleFor(x => x.UserAgent)
            .Length(1, 500)
            .WithMessage("User agent cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.UserAgent));

        // SECURITY RULE: Client IP should be valid format if provided
        RuleFor(x => x.ClientIpAddress)
            .Must(BeValidIpAddress)
            .WithMessage("Client IP address format is invalid")
            .When(x => !string.IsNullOrEmpty(x.ClientIpAddress));
    }

    private static bool NotContainSqlInjectionPatterns(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;

        // Common SQL injection patterns to block
        var dangerousPatterns = new[]
        {
            "'", "\"", ";", "--", "/*", "*/", "xp_", "sp_", 
            "exec", "execute", "select", "insert", "update", "delete", "drop", "create",
            "union", "having", "where", "order by", "group by",
            "script", "javascript", "vbscript", "<", ">"
        };

        return !dangerousPatterns.Any(pattern => 
            value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool NotContainWildcardAbuse(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm)) return true;

        // Prevent excessive wildcard usage that could impact performance
        var wildcardCount = searchTerm.Count(c => c == '*' || c == '%' || c == '?');
        var consecutiveWildcards = searchTerm.Contains("**") || searchTerm.Contains("%%");
        var onlyWildcards = searchTerm.All(c => c == '*' || c == '%' || c == '?' || char.IsWhiteSpace(c));

        return wildcardCount <= 3 && !consecutiveWildcards && !onlyWildcards;
    }

    private static bool BeValidSortOption(string sortBy)
    {
        if (string.IsNullOrEmpty(sortBy)) return true;

        var validOptions = new[]
        {
            "title", "title-asc", "title-desc",
            "date", "date-asc", "date-desc",
            "priority", "priority-asc", "priority-desc",
            "featured", "category", "relevance"
        };

        return validOptions.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeValidGuid(string value)
    {
        return string.IsNullOrEmpty(value) || Guid.TryParse(value, out _);
    }

    private static bool BeValidIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return true;

        // Basic IP address validation (IPv4 and IPv6)
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
}

/// <summary>
/// Validator for GetServiceBySlugRequest to ensure slug safety and prevent directory traversal
/// Focuses on security validation for slug-based service lookups
/// </summary>
public class GetServiceBySlugRequestValidator : AbstractValidator<GetServiceBySlugRequest>
{
    public GetServiceBySlugRequestValidator()
    {
        // SECURITY RULE: Slug must be safe and within reasonable bounds
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Service slug is required")
            .Length(1, 100)
            .WithMessage("Service slug must be 1-100 characters")
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("Service slug must contain only lowercase letters, numbers, and hyphens")
            .Must(NotContainDirectoryTraversal)
            .WithMessage("Service slug contains potentially dangerous path characters");

        // AUDIT COMPLIANCE: Request ID should be valid if provided  
        RuleFor(x => x.RequestId)
            .Must(BeValidGuid)
            .WithMessage("RequestId must be a valid GUID")
            .When(x => !string.IsNullOrEmpty(x.RequestId));
    }

    private static bool NotContainDirectoryTraversal(string slug)
    {
        if (string.IsNullOrEmpty(slug)) return true;

        // Prevent directory traversal and other dangerous patterns
        var dangerousPatterns = new[]
        {
            "..", "/", "\\", ":", "*", "?", "\"", "<", ">", "|",
            "%", "&", "+", "=", "@", "#", "!", "~", "`"
        };

        return !dangerousPatterns.Any(pattern => slug.Contains(pattern));
    }

    private static bool BeValidGuid(string value)
    {
        return string.IsNullOrEmpty(value) || Guid.TryParse(value, out _);
    }
}

/// <summary>
/// Validator for service category requests to ensure category safety
/// Lightweight validation for category-based queries
/// </summary>
public class GetServiceCategoriesRequestValidator : AbstractValidator<GetServiceCategoriesRequest>
{
    public GetServiceCategoriesRequestValidator()
    {
        // AUDIT COMPLIANCE: Request ID should be valid if provided
        RuleFor(x => x.RequestId)
            .Must(BeValidGuid)
            .WithMessage("RequestId must be a valid GUID")
            .When(x => !string.IsNullOrEmpty(x.RequestId));

        // SECURITY RULE: User agent should not be excessively long
        RuleFor(x => x.UserAgent)
            .Length(1, 500)
            .WithMessage("User agent cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.UserAgent));
    }

    private static bool BeValidGuid(string value)
    {
        return string.IsNullOrEmpty(value) || Guid.TryParse(value, out _);
    }
}

// Placeholder request classes that need to be defined based on actual use cases
public class GetServiceBySlugRequest
{
    public string Slug { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string? UserAgent { get; set; }
    public string? ClientIpAddress { get; set; }
}

public class GetServiceCategoriesRequest  
{
    public string? RequestId { get; set; }
    public string? UserAgent { get; set; }
    public bool ActiveOnly { get; set; } = true;
}
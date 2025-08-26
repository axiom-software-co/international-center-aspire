namespace Service.Audit.Configuration;

public sealed class AuditServiceOptionsValidator : AbstractValidator<AuditServiceOptions>
{
    public AuditServiceOptionsValidator()
    {
        RuleFor(x => x.SigningAlgorithm)
            .NotEmpty()
            .WithMessage("Signing algorithm is required for tamper-proof auditing")
            .Must(BeValidAlgorithm)
            .WithMessage("Signing algorithm must be one of: HMACSHA256, HMACSHA512, RSA");
            
        RuleFor(x => x.SigningKey)
            .NotEmpty()
            .When(x => x.RequireSignatures)
            .WithMessage("Signing key is required when signatures are enabled");
            
        RuleFor(x => x.MaxAuditRetentionDays)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Audit retention must be at least 1 day")
            .LessThanOrEqualTo(3650) // 10 years maximum
            .WithMessage("Audit retention cannot exceed 10 years");
            
        RuleFor(x => x.BatchSize)
            .GreaterThanOrEqualTo(100)
            .WithMessage("Batch size must be at least 100")
            .LessThanOrEqualTo(10000)
            .WithMessage("Batch size cannot exceed 10,000");
            
        RuleFor(x => x.DefaultTimeout)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Default timeout must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(5))
            .WithMessage("Default timeout cannot exceed 5 minutes");
            
        RuleFor(x => x.CleanupInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromHours(1))
            .WithMessage("Cleanup interval must be at least 1 hour")
            .When(x => x.EnableBackgroundCleanup);
    }
    
    private static bool BeValidAlgorithm(string algorithm)
    {
        var validAlgorithms = new[] { "HMACSHA256", "HMACSHA512", "RSA" };
        return validAlgorithms.Contains(algorithm, StringComparer.OrdinalIgnoreCase);
    }
}
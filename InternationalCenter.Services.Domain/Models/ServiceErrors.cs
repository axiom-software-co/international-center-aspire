namespace InternationalCenter.Services.Domain.Models;

public sealed class ValidationError : DomainError
{
    public ValidationError(string message, string? field = null) 
        : base("VALIDATION_ERROR", message)
    {
        Field = field;
    }

    public string? Field { get; }
}

public sealed class ServiceNotFoundError : DomainError
{
    public ServiceNotFoundError(string identifier) 
        : base("SERVICE_NOT_FOUND", $"Service with identifier '{identifier}' was not found")
    {
        Identifier = identifier;
    }

    public string Identifier { get; }
}

public sealed class ServiceQueryError : DomainError
{
    public ServiceQueryError(string message, Exception? innerException = null) 
        : base("SERVICE_QUERY_ERROR", message, ErrorSeverity.Error, innerException)
    {
    }
}
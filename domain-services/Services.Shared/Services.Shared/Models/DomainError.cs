namespace Services.Shared.Models;

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public class DomainError
{
    public string Code { get; }
    public string Message { get; }
    public ErrorSeverity Severity { get; }
    public Exception? InnerException { get; }

    public DomainError(string code, string message, ErrorSeverity severity = ErrorSeverity.Error, Exception? innerException = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Severity = severity;
        InnerException = innerException;
    }

    public override string ToString() => $"[{Code}] {Message}";
}

public static class Error
{
    public static DomainError Create(string code, string message, ErrorSeverity severity = ErrorSeverity.Error, Exception? innerException = null)
    {
        return new DomainError(code, message, severity, innerException);
    }
}
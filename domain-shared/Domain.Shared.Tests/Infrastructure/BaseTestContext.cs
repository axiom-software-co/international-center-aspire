using InternationalCenter.Shared.Tests.Abstractions;

namespace InternationalCenter.Shared.Tests.Infrastructure;

/// <summary>
/// Base implementation of test context providing common functionality
/// Implements standard test context behavior with validation and metadata management
/// Medical-grade test context with consistent identification and tracking
/// </summary>
public abstract class BaseTestContext : ITestContext
{
    private readonly Dictionary<string, object> _metadata;

    /// <summary>
    /// Gets the unique identifier for this test context instance
    /// </summary>
    public string ContextId { get; }

    /// <summary>
    /// Gets the test execution timestamp
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets or sets additional context metadata
    /// </summary>
    public IDictionary<string, object> Metadata => _metadata;

    protected BaseTestContext()
    {
        ContextId = GenerateContextId();
        CreatedAt = DateTime.UtcNow;
        _metadata = new Dictionary<string, object>();
    }

    protected BaseTestContext(string contextId)
    {
        if (string.IsNullOrWhiteSpace(contextId))
        {
            throw new ArgumentException("Context ID cannot be null or whitespace", nameof(contextId));
        }

        ContextId = contextId;
        CreatedAt = DateTime.UtcNow;
        _metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// Validates that the context is in a valid state
    /// Override in derived classes to provide domain-specific validation
    /// </summary>
    public virtual bool IsValid()
    {
        // Base validation checks
        if (string.IsNullOrWhiteSpace(ContextId))
            return false;

        if (CreatedAt == default)
            return false;

        if (CreatedAt > DateTime.UtcNow.AddMinutes(1))
            return false; // Context created in the future is invalid

        // Perform additional validation in derived classes
        return ValidateContextState();
    }

    /// <summary>
    /// Override this method to provide domain-specific validation logic
    /// </summary>
    protected virtual bool ValidateContextState()
    {
        return true; // Default implementation accepts all states
    }

    /// <summary>
    /// Generates a unique context identifier
    /// </summary>
    protected virtual string GenerateContextId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1000, 9999);
        var typeName = GetType().Name.Replace("TestContext", "").Replace("Context", "");
        
        return $"{typeName}_{timestamp}_{random}";
    }

    /// <summary>
    /// Adds metadata to the test context
    /// </summary>
    public void AddMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        _metadata[key] = value;
    }

    /// <summary>
    /// Gets metadata value by key
    /// </summary>
    public T? GetMetadata<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _metadata.TryGetValue(key, out var value) && value is T typedValue 
            ? typedValue 
            : default;
    }

    /// <summary>
    /// Checks if metadata exists for the given key
    /// </summary>
    public bool HasMetadata(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _metadata.ContainsKey(key);
    }

    /// <summary>
    /// Removes metadata by key
    /// </summary>
    public bool RemoveMetadata(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _metadata.Remove(key);
    }

    /// <summary>
    /// Clears all metadata
    /// </summary>
    public void ClearMetadata()
    {
        _metadata.Clear();
    }

    public override string ToString()
    {
        return $"{GetType().Name} [ID: {ContextId}, Created: {CreatedAt:yyyy-MM-dd HH:mm:ss} UTC, Metadata: {_metadata.Count} items]";
    }
}

/// <summary>
/// Simple test context implementation for basic testing scenarios
/// Provides minimal test context functionality without domain-specific requirements
/// </summary>
public class SimpleTestContext : BaseTestContext
{
    public string TestName { get; set; } = "Unknown";
    public string TestCategory { get; set; } = "General";

    public SimpleTestContext() : base()
    {
    }

    public SimpleTestContext(string testName, string testCategory = "General") : base()
    {
        TestName = testName ?? "Unknown";
        TestCategory = testCategory ?? "General";
        
        AddMetadata("TestName", TestName);
        AddMetadata("TestCategory", TestCategory);
    }

    protected override bool ValidateContextState()
    {
        return !string.IsNullOrWhiteSpace(TestName) && 
               !string.IsNullOrWhiteSpace(TestCategory);
    }
}

/// <summary>
/// Database-aware test context for integration testing scenarios
/// Provides database connection and transaction management for testing
/// </summary>
public class DatabaseTestContext : BaseTestContext, IAsyncDisposable
{
    private bool _disposed;

    public string? ConnectionString { get; set; }
    public string? DatabaseName { get; set; }
    public bool IsTransactionActive { get; protected set; }

    public DatabaseTestContext() : base()
    {
    }

    public DatabaseTestContext(string connectionString, string databaseName) : base()
    {
        ConnectionString = connectionString;
        DatabaseName = databaseName;
        
        AddMetadata("ConnectionString", ConnectionString);
        AddMetadata("DatabaseName", DatabaseName);
    }

    protected override bool ValidateContextState()
    {
        return !string.IsNullOrWhiteSpace(ConnectionString) && 
               !string.IsNullOrWhiteSpace(DatabaseName);
    }

    /// <summary>
    /// Marks that a transaction has been started
    /// </summary>
    public void BeginTransaction()
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        
        if (IsTransactionActive)
        {
            throw new InvalidOperationException("Transaction is already active");
        }

        IsTransactionActive = true;
        AddMetadata("TransactionStartTime", DateTime.UtcNow);
    }

    /// <summary>
    /// Marks that the transaction has been committed
    /// </summary>
    public void CommitTransaction()
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        
        if (!IsTransactionActive)
        {
            throw new InvalidOperationException("No active transaction to commit");
        }

        IsTransactionActive = false;
        AddMetadata("TransactionCommitTime", DateTime.UtcNow);
    }

    /// <summary>
    /// Marks that the transaction has been rolled back
    /// </summary>
    public void RollbackTransaction()
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        
        if (!IsTransactionActive)
        {
            throw new InvalidOperationException("No active transaction to rollback");
        }

        IsTransactionActive = false;
        AddMetadata("TransactionRollbackTime", DateTime.UtcNow);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        // Rollback any active transaction during disposal
        if (IsTransactionActive)
        {
            RollbackTransaction();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// HTTP-aware test context for API testing scenarios
/// Provides HTTP client configuration and request/response tracking
/// </summary>
public class HttpTestContext : BaseTestContext, IDisposable
{
    private bool _disposed;

    public HttpClient? HttpClient { get; set; }
    public string? BaseAddress { get; set; }
    public Dictionary<string, string> DefaultHeaders { get; } = new();
    public List<HttpRequestMessage> RequestHistory { get; } = [];
    public List<HttpResponseMessage> ResponseHistory { get; } = [];

    public HttpTestContext() : base()
    {
    }

    public HttpTestContext(HttpClient httpClient, string? baseAddress = null) : base()
    {
        HttpClient = httpClient;
        BaseAddress = baseAddress ?? httpClient?.BaseAddress?.ToString();
        
        if (BaseAddress != null)
        {
            AddMetadata("BaseAddress", BaseAddress);
        }
    }

    protected override bool ValidateContextState()
    {
        return HttpClient != null;
    }

    /// <summary>
    /// Adds a default header to all requests
    /// </summary>
    public void AddDefaultHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        DefaultHeaders[name] = value;
        HttpClient?.DefaultRequestHeaders.Add(name, value);
    }

    /// <summary>
    /// Records a request for testing history
    /// </summary>
    public void RecordRequest(HttpRequestMessage request)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        RequestHistory.Add(request);
        AddMetadata($"Request_{RequestHistory.Count}_Time", DateTime.UtcNow);
        AddMetadata($"Request_{RequestHistory.Count}_Method", request.Method.Method);
        AddMetadata($"Request_{RequestHistory.Count}_Uri", request.RequestUri?.ToString() ?? "Unknown");
    }

    /// <summary>
    /// Records a response for testing history
    /// </summary>
    public void RecordResponse(HttpResponseMessage response)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(response);

        ResponseHistory.Add(response);
        AddMetadata($"Response_{ResponseHistory.Count}_Time", DateTime.UtcNow);
        AddMetadata($"Response_{ResponseHistory.Count}_StatusCode", (int)response.StatusCode);
        AddMetadata($"Response_{ResponseHistory.Count}_ReasonPhrase", response.ReasonPhrase ?? "Unknown");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Dispose response history
        foreach (var response in ResponseHistory)
        {
            response?.Dispose();
        }
        ResponseHistory.Clear();

        // Dispose request history
        foreach (var request in RequestHistory)
        {
            request?.Dispose();
        }
        RequestHistory.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
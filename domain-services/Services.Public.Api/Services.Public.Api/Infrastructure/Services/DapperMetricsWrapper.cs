using Dapper;
using Npgsql;
using System.Data;
using System.Diagnostics;

namespace Services.Public.Api.Infrastructure.Services;

public class DapperMetricsWrapper : IDisposable
{
    private readonly ServicesPublicApiMetricsService _metricsService;
    private readonly ILogger<DapperMetricsWrapper> _logger;
    private readonly string _connectionString;

    public DapperMetricsWrapper(
        ServicesPublicApiMetricsService metricsService,
        ILogger<DapperMetricsWrapper> logger,
        string connectionString)
    {
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<IDbConnection> GetConnectionAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            success = true;

            stopwatch.Stop();
            _metricsService.RecordDapperConnection(stopwatch.Elapsed.TotalSeconds, success);

            return new MetricsDbConnectionWrapper(connection, _metricsService, _logger);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsService.RecordDapperConnection(stopwatch.Elapsed.TotalSeconds, success);
            _logger.LogError(ex, "Failed to open database connection");
            throw;
        }
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        using var connection = await GetConnectionAsync();
        return await QuerySingleOrDefaultInternalAsync<T>(connection, sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        using var connection = await GetConnectionAsync();
        return await QueryInternalAsync<T>(connection, sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        using var connection = await GetConnectionAsync();
        return await ExecuteInternalAsync(connection, sql, param, transaction, commandTimeout, commandType);
    }

    private async Task<T?> QuerySingleOrDefaultInternalAsync<T>(IDbConnection connection, string sql, object? param, IDbTransaction? transaction, int? commandTimeout, CommandType? commandType)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var result = await connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Dapper QuerySingleOrDefaultAsync failed: {Sql}", TruncateSql(sql));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordDapperQuery("SELECT", "QuerySingleOrDefault", stopwatch.Elapsed.TotalSeconds, success, success ? 1 : 0);
        }
    }

    private async Task<IEnumerable<T>> QueryInternalAsync<T>(IDbConnection connection, string sql, object? param, IDbTransaction? transaction, int? commandTimeout, CommandType? commandType)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var resultCount = 0;

        try
        {
            var results = await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
            var resultList = results.ToList();
            resultCount = resultList.Count;
            success = true;
            return resultList;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Dapper QueryAsync failed: {Sql}", TruncateSql(sql));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordDapperQuery("SELECT", "Query", stopwatch.Elapsed.TotalSeconds, success, resultCount);
        }
    }

    private async Task<int> ExecuteInternalAsync(IDbConnection connection, string sql, object? param, IDbTransaction? transaction, int? commandTimeout, CommandType? commandType)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var affectedRows = 0;

        try
        {
            affectedRows = await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
            success = true;
            return affectedRows;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Dapper ExecuteAsync failed: {Sql}", TruncateSql(sql));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var operation = DetermineOperation(sql);
            _metricsService.RecordDapperQuery(operation, "Execute", stopwatch.Elapsed.TotalSeconds, success, affectedRows);
        }
    }

    private static string DetermineOperation(string sql)
    {
        var trimmedSql = sql.Trim().ToUpperInvariant();
        
        if (trimmedSql.StartsWith("SELECT")) return "SELECT";
        if (trimmedSql.StartsWith("INSERT")) return "INSERT";
        if (trimmedSql.StartsWith("UPDATE")) return "UPDATE";
        if (trimmedSql.StartsWith("DELETE")) return "DELETE";
        if (trimmedSql.StartsWith("EXEC") || trimmedSql.StartsWith("CALL")) return "PROCEDURE";
        
        return "OTHER";
    }

    private static string TruncateSql(string sql)
    {
        if (string.IsNullOrEmpty(sql)) return "";
        
        // Remove sensitive data and truncate for logging
        var cleanSql = sql.Replace('\n', ' ').Replace('\r', ' ');
        return cleanSql.Length > 200 ? cleanSql[..197] + "..." : cleanSql;
    }

    public void Dispose()
    {
        _logger.LogDebug("DapperMetricsWrapper disposed");
    }
}

internal class MetricsDbConnectionWrapper : IDbConnection
{
    private readonly IDbConnection _connection;
    private readonly ServicesPublicApiMetricsService _metricsService;
    private readonly ILogger _logger;

    public MetricsDbConnectionWrapper(IDbConnection connection, ServicesPublicApiMetricsService metricsService, ILogger logger)
    {
        _connection = connection;
        _metricsService = metricsService;
        _logger = logger;
    }

    public void Dispose()
    {
        try
        {
            _connection?.Dispose();
            _metricsService.RecordDapperConnectionClosed();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occurred while disposing database connection");
        }
    }

    // Delegate all other IDbConnection members to the wrapped connection
    public string ConnectionString
    {
        get => _connection.ConnectionString;
        set => _connection.ConnectionString = value;
    }

    public int ConnectionTimeout => _connection.ConnectionTimeout;
    public string Database => _connection.Database;
    public ConnectionState State => _connection.State;

    public IDbTransaction BeginTransaction() => _connection.BeginTransaction();
    public IDbTransaction BeginTransaction(IsolationLevel il) => _connection.BeginTransaction(il);
    public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);
    public void Close() => _connection.Close();
    public IDbCommand CreateCommand() => _connection.CreateCommand();
    public void Open() => _connection.Open();
}
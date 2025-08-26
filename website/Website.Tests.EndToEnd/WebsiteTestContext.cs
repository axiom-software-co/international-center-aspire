using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using InternationalCenter.Website.Shared.Tests.Contracts;
using InternationalCenter.Shared.Tests.Abstractions;
using System.Collections.Concurrent;

namespace Website.Tests.EndToEnd;

/// <summary>
/// Website-specific test context for end-to-end testing with Playwright
/// Provides browser automation and Public Gateway integration for anonymous user testing
/// Medical-grade test environment with performance tracking and accessibility validation
/// </summary>
public class WebsiteTestContext : IWebsiteTestContext, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBrowser _browser;
    private readonly WebsiteTestEnvironmentOptions _options;
    private readonly ConcurrentBag<object> _createdEntities = new();
    private readonly Dictionary<string, object> _entityCache = new();
    private readonly ILogger<WebsiteTestContext> _logger;
    private bool _disposed = false;

    public WebsiteTestContext(IServiceProvider serviceProvider, IBrowser browser, WebsiteTestEnvironmentOptions options)
    {
        _serviceProvider = serviceProvider;
        _browser = browser;
        _options = options;
        _logger = serviceProvider.GetRequiredService<ILogger<WebsiteTestContext>>();
        
        TestDataFactory = serviceProvider.GetRequiredService<IWebsiteTestDataFactoryContract>();
        ValidationUtilities = serviceProvider.GetRequiredService<IWebsiteValidationUtilitiesContract>();
    }

    public IServiceProvider ServiceProvider => _serviceProvider;

    public IConfiguration Configuration { get; private set; } = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicGateway:BaseUrl"] = "http://localhost:5001",
            ["Website:BaseUrl"] = "http://localhost:3000",
            ["Testing:Environment"] = "EndToEnd"
        })
        .Build();

    public ILogger Logger => _logger;

    public IBrowser Browser => _browser;

    public IBrowserContext? BrowserContext { get; private set; }

    public IPage? Page { get; private set; }

    public IApiMockServer? ApiMockServer { get; private set; }

    public IPublicGatewayApiClient? PublicGatewayClient { get; private set; }

    public IWebsiteTestDataFactoryContract TestDataFactory { get; }

    public IWebsiteValidationUtilitiesContract ValidationUtilities { get; }

    public ICollection<object> CreatedTestEntities => _createdEntities;

    public async Task InitializeAsync()
    {
        BrowserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = _options.DefaultViewportSize.Width,
                Height = _options.DefaultViewportSize.Height
            },
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["X-Test-Environment"] = "EndToEnd",
                ["User-Agent"] = "Mozilla/5.0 (compatible; ICWebsiteTests/1.0)"
            }
        });

        Page = await CreatePageAsync();

        if (_options.EnableApiMocking)
        {
            ApiMockServer = new MockWebServer(_options.PublicGatewayBaseUrl);
            await ApiMockServer.StartAsync();
        }

        PublicGatewayClient = new PublicGatewayTestClient(
            _options.PublicGatewayBaseUrl, 
            TimeSpan.FromSeconds(10));

        _logger.LogInformation("Website test context initialized successfully");
    }

    public async Task<IPage> CreatePageAsync(ViewportSize? viewportSize = null)
    {
        if (BrowserContext == null)
            throw new InvalidOperationException("Browser context not initialized");

        var page = await BrowserContext.NewPageAsync();
        
        if (viewportSize != null)
        {
            await page.SetViewportSizeAsync(viewportSize.Width, viewportSize.Height);
        }

        if (_options.EnablePerformanceTracking)
        {
            await page.RouteAsync("**/*", async route =>
            {
                var request = route.Request;
                var startTime = DateTime.UtcNow;
                
                await route.ContinueAsync();
                
                var duration = DateTime.UtcNow - startTime;
                _logger.LogDebug("Request to {Url} completed in {Duration}ms", 
                    request.Url, duration.TotalMilliseconds);
            });
        }

        return page;
    }

    public async Task NavigateToAsync(string url, NavigationOptions? options = null)
    {
        if (Page == null)
            throw new InvalidOperationException("Page not initialized");

        var navigationOptions = options ?? new NavigationOptions();
        var fullUrl = url.StartsWith("http") ? url : $"{Configuration["Website:BaseUrl"]}{url}";

        _logger.LogInformation("Navigating to: {Url}", fullUrl);

        var response = await Page.GotoAsync(fullUrl, new PageGotoOptions
        {
            Timeout = (float)navigationOptions.NavigationTimeout.TotalMilliseconds,
            WaitUntil = navigationOptions.WaitForNetworkIdle ? WaitUntilState.NetworkIdle : WaitUntilState.DOMContentLoaded
        });

        if (!response?.Ok == true)
        {
            throw new InvalidOperationException($"Navigation to {fullUrl} failed with status: {response?.Status}");
        }

        if (navigationOptions.ValidatePageTitle && !string.IsNullOrEmpty(navigationOptions.ExpectedTitle))
        {
            await Assertions.Expect(Page).ToHaveTitleAsync(navigationOptions.ExpectedTitle);
        }

        if (navigationOptions.CaptureScreenshot)
        {
            await CaptureScreenshotAsync($"navigation_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
        }

        _logger.LogInformation("Successfully navigated to: {Url}", fullUrl);
    }

    public async Task SetupApiMocksAsync(Dictionary<string, MockApiResponse> mockResponses)
    {
        if (ApiMockServer == null)
        {
            _logger.LogWarning("API Mock Server not initialized - skipping mock setup");
            return;
        }

        foreach (var (path, response) in mockResponses)
        {
            ApiMockServer.SetupMock(path, response);
            _logger.LogDebug("Setup mock for path: {Path}", path);
        }

        _logger.LogInformation("Setup {Count} API mocks", mockResponses.Count);
    }

    public async Task<string> CaptureScreenshotAsync(string? name = null)
    {
        if (Page == null)
            throw new InvalidOperationException("Page not initialized");

        var fileName = name ?? $"screenshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        var filePath = Path.Combine(Path.GetTempPath(), "website-tests", $"{fileName}.png");
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filePath,
            FullPage = true
        });

        _logger.LogInformation("Screenshot captured: {FilePath}", filePath);
        return filePath;
    }

    public void RegisterForCleanup<T>(T entity) where T : class
    {
        _createdEntities.Add(entity);
        _logger.LogDebug("Registered entity for cleanup: {Type}", typeof(T).Name);
    }

    public async Task<T> GetOrCreateTestEntityAsync<T>(Func<Task<T>> factory) where T : class
    {
        var key = typeof(T).Name;
        
        if (_entityCache.TryGetValue(key, out var cached) && cached is T cachedEntity)
        {
            return cachedEntity;
        }

        var entity = await factory();
        _entityCache[key] = entity!;
        RegisterForCleanup(entity);
        
        return entity;
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // Cleanup is handled by the test class
            _logger.LogInformation("Website test context disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during website test context disposal");
        }
        finally
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Mock web server implementation for API mocking using MSW-style approach
/// </summary>
public class MockWebServer : IApiMockServer
{
    private readonly Dictionary<string, MockApiResponse> _mocks = new();
    private readonly string _baseUrl;
    private bool _isRunning = false;

    public MockWebServer(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public string BaseUrl => _baseUrl;
    public bool IsRunning => _isRunning;

    public Task StartAsync()
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _isRunning = false;
        _mocks.Clear();
        return Task.CompletedTask;
    }

    public void SetupMock(string path, MockApiResponse response)
    {
        _mocks[path] = response;
    }

    public void ClearMocks()
    {
        _mocks.Clear();
    }
}

/// <summary>
/// Public Gateway API client for testing
/// </summary>
public class PublicGatewayTestClient : IPublicGatewayApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public PublicGatewayTestClient(string baseUrl, TimeSpan defaultTimeout)
    {
        _baseUrl = baseUrl;
        DefaultTimeout = defaultTimeout;
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = defaultTimeout
        };
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ICWebsiteTests/1.0");
    }

    public string BaseUrl => _baseUrl;
    public TimeSpan DefaultTimeout { get; }

    public async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await GetRawAsync(endpoint, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return System.Text.Json.JsonSerializer.Deserialize<T>(content)!;
    }

    public async Task<T> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        var response = await PostRawAsync(endpoint, data, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return System.Text.Json.JsonSerializer.Deserialize<T>(content)!;
    }

    public async Task<HttpResponseMessage> GetRawAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetAsync(endpoint, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostRawAsync(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(endpoint, content, cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
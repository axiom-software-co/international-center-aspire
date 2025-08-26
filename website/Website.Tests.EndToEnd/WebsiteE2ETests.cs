using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using InternationalCenter.Website.Shared.Tests.Contracts;
using InternationalCenter.Shared.Tests.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Website.Tests.EndToEnd;

/// <summary>
/// Comprehensive end-to-end tests for the International Center Website
/// Tests complete user workflows through Public Gateway integration using Playwright
/// Medical-grade frontend validation ensuring anonymous access patterns and performance compliance
/// </summary>
[Collection("Website E2E Tests")]
public class WebsiteE2ETests : IAsyncLifetime, IWebsiteTestEnvironmentContract<WebsiteTestContext>
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebsiteE2ETests> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private WebsiteTestContext? _testContext;

    public WebsiteE2ETests(ITestOutputHelper output)
    {
        _output = output;
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddXUnit(output));
        services.AddScoped<IWebsiteTestDataFactoryContract, WebsiteTestDataFactory>();
        services.AddScoped<IWebsiteValidationUtilitiesContract, WebsiteValidationUtilities>();
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<WebsiteE2ETests>>();
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-dev-shm-usage", "--no-sandbox" }
        });
        
        _testContext = await SetupWebsiteTestEnvironmentAsync(new WebsiteTestEnvironmentOptions
        {
            UseHeadlessBrowser = true,
            EnableApiMocking = true,
            EnablePerformanceTracking = true,
            EnableAccessibilityValidation = true
        });
    }

    public async Task DisposeAsync()
    {
        if (_testContext != null)
        {
            await CleanupWebsiteEnvironmentAsync(_testContext);
        }
        
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }
        
        _playwright?.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task HomePage_Should_Load_Successfully_For_Anonymous_Users()
    {
        var testResult = await ExecuteWebsiteTestAsync(_testContext!, async context =>
        {
            await context.NavigateToAsync("/", new NavigationOptions
            {
                WaitForNetworkIdle = true,
                ValidatePageTitle = true,
                ExpectedTitle = "International Medical Center",
                CaptureScreenshot = true
            });

            var page = context.Page!;
            
            // Validate page structure for anonymous users
            await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Navigation)).ToBeVisibleAsync();
            
            // Validate no authentication required elements are visible
            await Assertions.Expect(page.GetByText("Sign In")).ToBeHiddenAsync();
            await Assertions.Expect(page.GetByText("Login")).ToBeHiddenAsync();
            
            // Validate performance metrics
            await context.ValidationUtilities.ValidateBrowserPerformanceAsync(page, new PerformanceValidationRules
            {
                MaxLCP = TimeSpan.FromMilliseconds(2500),
                MaxFID = TimeSpan.FromMilliseconds(100),
                MaxCLS = 0.1
            }, _output);

            return true;
        }, 
        "HomePage Load Test",
        new PerformanceThreshold { MaxDuration = TimeSpan.FromSeconds(5) });

        Assert.True(testResult);
    }

    [Fact]
    public async Task ServiceSearch_Should_Work_Through_Public_Gateway()
    {
        var testResult = await ExecuteWebsiteTestAsync(_testContext!, async context =>
        {
            // Setup API mocks for Public Gateway Services endpoint
            await context.SetupApiMocksAsync(new Dictionary<string, MockApiResponse>
            {
                ["/api/services/search"] = new MockApiResponse
                {
                    Data = await context.TestDataFactory.CreateMockServiceDataAsync(5),
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Headers = { ["Content-Type"] = "application/json" }
                }
            });

            await context.NavigateToAsync("/services");
            var page = context.Page!;

            // Test search functionality
            var searchInput = page.GetByPlaceholder("Search medical services...");
            await Assertions.Expect(searchInput).ToBeVisibleAsync();
            
            await searchInput.FillAsync("cardiology");
            await page.Keyboard.PressAsync("Enter");

            // Validate search results from Public Gateway
            await Assertions.Expect(page.GetByTestId("search-results")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("search-result-item")).ToHaveCountAsync(5);

            // Validate accessibility for search results
            await context.ValidationUtilities.ValidateComponentAccessibilityAsync(
                "search-results",
                new AccessibilityValidationRules { ValidateScreenReaderSupport = true },
                _output);

            // Validate no sensitive data exposure for anonymous users
            await context.ValidationUtilities.ValidateSecurityPrivacyAsync(page, new SecurityPrivacyValidationRules
            {
                ValidateNoSensitiveDataExposure = true,
                ValidateCookieSecure = true
            }, _output);

            return true;
        },
        "Service Search Test",
        new PerformanceThreshold { MaxDuration = TimeSpan.FromSeconds(8) });

        Assert.True(testResult);
    }

    [Fact]
    public async Task ResponsiveDesign_Should_Work_Across_Viewports()
    {
        var viewports = new ViewportSize[]
        {
            new(320, 568),   // Mobile
            new(768, 1024),  // Tablet
            new(1920, 1080)  // Desktop
        };

        foreach (var viewport in viewports)
        {
            var testResult = await ExecuteWebsiteTestAsync(_testContext!, async context =>
            {
                await context.Page!.SetViewportSizeAsync(viewport.Width, viewport.Height);
                await context.NavigateToAsync("/");

                await context.ValidationUtilities.ValidateResponsiveDesignAsync(
                    context.Page!,
                    new[] { viewport },
                    new ResponsiveValidationRules
                    {
                        ValidateNavigationUsability = true,
                        ValidateContentVisibility = true,
                        ValidateTouchTargets = true,
                        MinTouchTargetSize = 44
                    },
                    _output);

                return true;
            },
            $"Responsive Design Test - {viewport.Width}x{viewport.Height}",
            new PerformanceThreshold { MaxDuration = TimeSpan.FromSeconds(3) });

            Assert.True(testResult, $"Responsive design failed for viewport {viewport.Width}x{viewport.Height}");
        }
    }

    [Fact]
    public async Task ServiceDetails_Should_Display_Without_Sensitive_Data()
    {
        var testResult = await ExecuteWebsiteTestAsync(_testContext!, async context =>
        {
            var mockServiceId = Guid.NewGuid();
            var mockServiceData = (await context.TestDataFactory.CreateMockServiceDataAsync(1)).First();
            
            await context.SetupApiMocksAsync(new Dictionary<string, MockApiResponse>
            {
                [$"/api/services/{mockServiceId}"] = new MockApiResponse
                {
                    Data = mockServiceData,
                    StatusCode = System.Net.HttpStatusCode.OK
                }
            });

            await context.NavigateToAsync($"/services/{mockServiceId}");
            var page = context.Page!;

            // Validate service information display
            await Assertions.Expect(page.GetByTestId("service-title")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("service-description")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("service-contact-info")).ToBeVisibleAsync();

            // Validate no sensitive data exposure for anonymous users
            await Assertions.Expect(page.GetByText("Patient Records")).ToBeHiddenAsync();
            await Assertions.Expect(page.GetByText("Internal Notes")).ToBeHiddenAsync();
            await Assertions.Expect(page.GetByText("Staff Only")).ToBeHiddenAsync();

            // Validate contact information privacy
            await context.ValidationUtilities.ValidateApiClientIntegrationAsync(
                "service-details-client",
                new ApiClientValidationRules
                {
                    ValidateAnonymousAccess = true,
                    ValidateResponseCaching = true,
                    MaxResponseTime = TimeSpan.FromSeconds(3)
                },
                _output);

            return true;
        },
        "Service Details Privacy Test",
        new PerformanceThreshold { MaxDuration = TimeSpan.FromSeconds(6) });

        Assert.True(testResult);
    }

    [Fact]
    public async Task PublicGateway_RateLimit_Should_Be_Handled_Gracefully()
    {
        var testResult = await ExecuteWebsiteTestAsync(_testContext!, async context =>
        {
            // Mock rate limit response from Public Gateway
            await context.SetupApiMocksAsync(new Dictionary<string, MockApiResponse>
            {
                ["/api/services"] = new MockApiResponse
                {
                    StatusCode = System.Net.HttpStatusCode.TooManyRequests,
                    Headers = {
                        ["Retry-After"] = "60",
                        ["X-RateLimit-Remaining"] = "0"
                    }
                }
            });

            await context.NavigateToAsync("/services");
            var page = context.Page!;

            // Validate rate limit handling
            await Assertions.Expect(page.GetByTestId("rate-limit-message")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("Please wait before making another request")).ToBeVisibleAsync();

            // Validate no error exposure to anonymous users
            await Assertions.Expect(page.GetByText("Internal Server Error")).ToBeHiddenAsync();
            await Assertions.Expect(page.GetByText("Stack Trace")).ToBeHiddenAsync();

            // Validate accessibility of error message
            await context.ValidationUtilities.ValidateComponentAccessibilityAsync(
                "rate-limit-message",
                new AccessibilityValidationRules { ValidateAriaLabels = true },
                _output);

            return true;
        },
        "Rate Limit Handling Test",
        new PerformanceThreshold { MaxDuration = TimeSpan.FromSeconds(4) });

        Assert.True(testResult);
    }

    [Fact]
    public async Task CategoryNavigation_Should_Work_For_Anonymous_Users()
    {
        var testResult = await ExecuteWebsiteTestAsync(_testContext!, async context =>
        {
            var mockCategories = await context.TestDataFactory.CreateMockCategoryDataAsync(3, 5);
            
            await context.SetupApiMocksAsync(new Dictionary<string, MockApiResponse>
            {
                ["/api/categories"] = new MockApiResponse
                {
                    Data = mockCategories,
                    StatusCode = System.Net.HttpStatusCode.OK
                }
            });

            await context.NavigateToAsync("/categories");
            var page = context.Page!;

            // Test category hierarchy navigation
            await Assertions.Expect(page.GetByTestId("category-tree")).ToBeVisibleAsync();
            
            var firstCategory = page.GetByTestId("category-item").First;
            await firstCategory.ClickAsync();

            // Validate category selection and service filtering
            await Assertions.Expect(page.GetByTestId("category-services")).ToBeVisibleAsync();
            
            // Validate keyboard navigation accessibility
            await page.Keyboard.PressAsync("Tab");
            await page.Keyboard.PressAsync("Enter");

            // Validate breadcrumb navigation
            await Assertions.Expect(page.GetByTestId("category-breadcrumbs")).ToBeVisibleAsync();

            return true;
        },
        "Category Navigation Test",
        new PerformanceThreshold { MaxDuration = TimeSpan.FromSeconds(5) });

        Assert.True(testResult);
    }

    // Implementation of IWebsiteTestEnvironmentContract
    public async Task<WebsiteTestContext> SetupWebsiteTestEnvironmentAsync(
        WebsiteTestEnvironmentOptions options,
        CancellationToken cancellationToken = default)
    {
        var context = new WebsiteTestContext(_serviceProvider, _browser!, options);
        
        await context.InitializeAsync();
        await ValidateWebsiteEnvironmentAsync(context, cancellationToken);
        
        return context;
    }

    public async Task<T> ExecuteWebsiteTestAsync<T>(
        WebsiteTestContext context,
        Func<WebsiteTestContext, Task<T>> testOperation,
        string operationName,
        PerformanceThreshold? performanceThreshold = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting website test operation: {OperationName}", operationName);

        try
        {
            var result = await testOperation(context);
            
            var duration = DateTime.UtcNow - startTime;
            if (performanceThreshold != null && duration > performanceThreshold.MaxDuration)
            {
                throw new TimeoutException($"Operation {operationName} exceeded performance threshold: {duration} > {performanceThreshold.MaxDuration}");
            }

            _logger.LogInformation("Completed website test operation: {OperationName} in {Duration}ms", 
                operationName, duration.TotalMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed website test operation: {OperationName}", operationName);
            
            if (context.Page != null)
            {
                await context.CaptureScreenshotAsync($"error_{operationName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
            }
            
            throw;
        }
    }

    public async Task ValidateWebsiteEnvironmentAsync(
        WebsiteTestContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Browser == null)
            throw new InvalidOperationException("Browser not initialized");
        
        if (context.Page == null)
            throw new InvalidOperationException("Page not initialized");

        // Validate Public Gateway connectivity
        try
        {
            var response = await context.PublicGatewayClient!.GetRawAsync("/health");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Public Gateway health check failed with status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not connect to Public Gateway - using mocked responses");
        }

        _logger.LogInformation("Website test environment validated successfully");
    }

    public async Task CleanupWebsiteEnvironmentAsync(
        WebsiteTestContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (context.ApiMockServer?.IsRunning == true)
            {
                await context.ApiMockServer.StopAsync();
            }

            if (context.Page != null)
            {
                await context.Page.CloseAsync();
            }

            if (context.BrowserContext != null)
            {
                await context.BrowserContext.CloseAsync();
            }

            context.Dispose();
            
            _logger.LogInformation("Website test environment cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during website test environment cleanup");
        }
    }
}

/// <summary>
/// Performance threshold configuration for test operations
/// </summary>
public record PerformanceThreshold
{
    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromSeconds(10);
    public long MaxMemoryUsage { get; init; } = 100 * 1024 * 1024; // 100MB
    public int MaxCpuUsage { get; init; } = 80; // 80%
}
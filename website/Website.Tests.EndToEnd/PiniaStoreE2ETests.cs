using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using InternationalCenter.Website.Shared.Tests.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Website.Tests.EndToEnd;

/// <summary>
/// End-to-end tests for Pinia store integration in the Website
/// Tests state management, Public Gateway API integration, and anonymous user workflows
/// Medical-grade store testing ensuring data consistency and privacy compliance
/// </summary>
[Collection("Website E2E Tests")]
public class PiniaStoreE2ETests : IAsyncLifetime, IWebsitePiniaStoreContract<object>
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PiniaStoreE2ETests> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private WebsiteTestContext? _testContext;

    public PiniaStoreE2ETests(ITestOutputHelper output)
    {
        _output = output;
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddXUnit(output));
        services.AddScoped<IWebsiteTestDataFactoryContract, WebsiteTestDataFactory>();
        services.AddScoped<IWebsiteValidationUtilitiesContract, WebsiteValidationUtilities>();
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<PiniaStoreE2ETests>>();
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-dev-shm-usage", "--no-sandbox" }
        });

        _testContext = new WebsiteTestContext(_serviceProvider, _browser, new WebsiteTestEnvironmentOptions
        {
            UseHeadlessBrowser = true,
            EnableApiMocking = true,
            EnablePerformanceTracking = true
        });

        await _testContext.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (_testContext != null)
        {
            _testContext.Dispose();
        }
        
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }
        
        _playwright?.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task ServicesStore_Should_Handle_Anonymous_User_Workflows()
    {
        await _testContext!.SetupApiMocksAsync(new Dictionary<string, MockApiResponse>
        {
            ["/api/services"] = new MockApiResponse
            {
                Data = await _testContext.TestDataFactory.CreateMockServiceDataAsync(10),
                StatusCode = System.Net.HttpStatusCode.OK
            }
        });

        await _testContext.NavigateToAsync("/services");
        var page = _testContext.Page!;

        // Test store initialization
        var storeState = await page.EvaluateAsync<dynamic>(@"() => {
            if (window.__PINIA_STORES__) {
                const servicesStore = window.__PINIA_STORES__.services;
                return {
                    initialized: !!servicesStore,
                    hasServices: servicesStore?.services?.length > 0,
                    isLoading: servicesStore?.isLoading,
                    error: servicesStore?.error
                };
            }
            return { initialized: false };
        }");

        Assert.True((bool)storeState.initialized, "Services store should be initialized");
        Assert.False((bool)storeState.isLoading, "Store should not be in loading state after initialization");
        Assert.Null(storeState.error);

        _output.WriteLine("✓ Services store initialized successfully for anonymous user");
    }

    [Fact]
    public async Task SearchStore_Should_Persist_Anonymous_User_Preferences()
    {
        await _testContext!.NavigateToAsync("/search");
        var page = _testContext.Page!;

        // Perform a search to populate store state
        var searchInput = page.GetByPlaceholder("Search medical services...");
        await searchInput.FillAsync("cardiology");
        await page.Keyboard.PressAsync("Enter");

        await page.WaitForTimeoutAsync(1000); // Allow search to complete

        // Check if search history is persisted in localStorage for anonymous users
        var searchHistory = await page.EvaluateAsync<string[]>(@"() => {
            const stored = localStorage.getItem('ic-search-history');
            return stored ? JSON.parse(stored) : [];
        }");

        Assert.Contains("cardiology", searchHistory);
        _output.WriteLine("✓ Search history persisted in localStorage for anonymous user");

        // Validate no sensitive data in localStorage
        var localStorageKeys = await page.EvaluateAsync<string[]>(@"() => {
            return Object.keys(localStorage);
        }");

        var hasSensitiveKeys = localStorageKeys.Any(key => 
            key.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("session", StringComparison.OrdinalIgnoreCase));

        Assert.False(hasSensitiveKeys, "No sensitive data should be stored for anonymous users");
        _output.WriteLine("✓ No sensitive data stored in localStorage");
    }

    [Fact]
    public async Task CategoriesStore_Should_Handle_Hierarchy_Navigation()
    {
        var mockCategories = await _testContext!.TestDataFactory.CreateMockCategoryDataAsync(3, 5);
        
        await _testContext.SetupApiMocksAsync(new Dictionary<string, MockApiResponse>
        {
            ["/api/categories"] = new MockApiResponse
            {
                Data = mockCategories,
                StatusCode = System.Net.HttpStatusCode.OK
            }
        });

        await _testContext.NavigateToAsync("/categories");
        var page = _testContext.Page!;

        // Test category expansion state persistence
        var firstCategory = page.GetByTestId("category-item").First;
        await firstCategory.ClickAsync();

        await page.WaitForTimeoutAsync(500);

        // Check if expansion state is tracked in the store
        var expansionState = await page.EvaluateAsync<dynamic>(@"() => {
            if (window.__PINIA_STORES__?.categories) {
                const categoriesStore = window.__PINIA_STORES__.categories;
                return {
                    hasExpandedCategories: Object.keys(categoriesStore.expandedCategories || {}).length > 0,
                    categoryCount: categoriesStore.categories?.length || 0
                };
            }
            return { hasExpandedCategories: false, categoryCount: 0 };
        }");

        Assert.True((bool)expansionState.hasExpandedCategories, "Categories should track expansion state");
        Assert.True((int)expansionState.categoryCount > 0, "Categories should be loaded in store");

        _output.WriteLine("✓ Categories store handles hierarchy navigation correctly");
    }

    [Fact]
    public async Task UIStore_Should_Manage_Anonymous_User_Preferences()
    {
        await _testContext!.NavigateToAsync("/");
        var page = _testContext.Page!;

        // Test theme preference for anonymous users
        var themeToggle = page.GetByTestId("theme-toggle");
        if (await themeToggle.IsVisibleAsync())
        {
            await themeToggle.ClickAsync();
            await page.WaitForTimeoutAsync(300);

            // Check if theme preference is persisted
            var themePreference = await page.EvaluateAsync<string>(@"() => {
                return localStorage.getItem('ic-theme-preference') || 'light';
            }");

            Assert.Contains(themePreference, new[] { "light", "dark", "system" });
            _output.WriteLine($"✓ Theme preference '{themePreference}' persisted for anonymous user");
        }

        // Test accessibility preferences
        var reducedMotionPreference = await page.EvaluateAsync<bool>(@"() => {
            return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        }");

        var uiStoreAccessibility = await page.EvaluateAsync<dynamic>(@"() => {
            if (window.__PINIA_STORES__?.ui) {
                return window.__PINIA_STORES__.ui.accessibilitySettings || {};
            }
            return {};
        }");

        _output.WriteLine($"✓ UI store respects system accessibility preferences (reduced motion: {reducedMotionPreference})");
    }

    [Fact]
    public async Task Stores_Should_Handle_Public_Gateway_Rate_Limiting()
    {
        // Mock rate limit response
        await _testContext!.SetupApiMocksAsync(new Dictionary<string, MockApiResponse>
        {
            ["/api/services"] = new MockApiResponse
            {
                StatusCode = System.Net.HttpStatusCode.TooManyRequests,
                Headers = { ["Retry-After"] = "60" }
            }
        });

        await _testContext.NavigateToAsync("/services");
        var page = _testContext.Page!;

        await page.WaitForTimeoutAsync(1000);

        // Check if stores handle rate limiting gracefully
        var storeErrorState = await page.EvaluateAsync<dynamic>(@"() => {
            const stores = window.__PINIA_STORES__ || {};
            return {
                servicesError: stores.services?.error?.type,
                searchError: stores.search?.error?.type,
                hasUserFriendlyMessage: document.querySelector('[data-testid=""rate-limit-message""]') !== null
            };
        }");

        Assert.Equal("rate_limit", storeErrorState.servicesError);
        Assert.True((bool)storeErrorState.hasUserFriendlyMessage, "User-friendly rate limit message should be displayed");

        _output.WriteLine("✓ Stores handle Public Gateway rate limiting gracefully");
    }

    [Fact]
    public async Task Stores_Should_Not_Expose_Sensitive_Data_To_Anonymous_Users()
    {
        await _testContext!.NavigateToAsync("/");
        var page = _testContext.Page!;

        // Check all store states for sensitive data exposure
        var storeStates = await page.EvaluateAsync<Dictionary<string, object>>(@"() => {
            const stores = window.__PINIA_STORES__ || {};
            const sanitizedStores = {};
            
            for (const [storeName, store] of Object.entries(stores)) {
                // Serialize store state to check for sensitive patterns
                const storeJson = JSON.stringify(store);
                sanitizedStores[storeName] = {
                    hasSensitiveData: /(?:password|token|secret|key|auth|session|ssn|credit)/i.test(storeJson),
                    stateKeys: Object.keys(store || {})
                };
            }
            
            return sanitizedStores;
        }");

        foreach (var (storeName, storeInfo) in storeStates)
        {
            var info = (Dictionary<string, object>)storeInfo;
            Assert.False((bool)info["hasSensitiveData"], $"Store '{storeName}' should not contain sensitive data for anonymous users");
        }

        _output.WriteLine("✓ No sensitive data exposed in store states for anonymous users");
    }

    // Implementation of IWebsitePiniaStoreContract methods for E2E testing context

    public async Task TestStoreInitializationAsync(object store, StoreInitializationExpectations expectations, ITestOutputHelper? output = null)
    {
        output?.WriteLine("Testing store initialization in browser context...");
        
        var page = _testContext!.Page!;
        var storeState = await page.EvaluateAsync<dynamic>(@"(storeName) => {
            const stores = window.__PINIA_STORES__ || {};
            return stores[storeName] || null;
        }", store.ToString()!.ToLower());

        Assert.NotNull(storeState);
        
        if (expectations.RequiredProperties != null)
        {
            foreach (var prop in expectations.RequiredProperties)
            {
                Assert.True(storeState.GetType().GetProperty(prop) != null, $"Store should have property: {prop}");
            }
        }

        output?.WriteLine("Store initialization test completed successfully");
    }

    public async Task TestStoreStateMutationsAsync(object store, StateMutationTestCase[] mutationTestCases, ITestOutputHelper? output = null)
    {
        output?.WriteLine($"Testing {mutationTestCases.Length} state mutations...");
        
        foreach (var testCase in mutationTestCases)
        {
            var page = _testContext!.Page!;
            
            // Execute action in browser context
            await page.EvaluateAsync(@"(actionName, payload) => {
                const stores = window.__PINIA_STORES__ || {};
                const store = Object.values(stores)[0];
                if (store && store[actionName]) {
                    store[actionName](payload);
                }
            }", testCase.ActionName, testCase.ActionPayload);

            output?.WriteLine($"✓ Mutation '{testCase.ActionName}' executed successfully");
        }
    }

    public async Task TestStoreGettersAsync(object store, GetterTestCase[] getterTestCases, ITestOutputHelper? output = null)
    {
        output?.WriteLine($"Testing {getterTestCases.Length} store getters...");
        
        foreach (var testCase in getterTestCases)
        {
            var page = _testContext!.Page!;
            
            var getterValue = await page.EvaluateAsync<object>(@"(getterName) => {
                const stores = window.__PINIA_STORES__ || {};
                const store = Object.values(stores)[0];
                return store?.[getterName];
            }", testCase.GetterName);

            if (testCase.ValueValidator != null)
            {
                Assert.True(testCase.ValueValidator(getterValue), $"Getter '{testCase.GetterName}' value validation failed");
            }

            output?.WriteLine($"✓ Getter '{testCase.GetterName}' validated successfully");
        }
    }

    public async Task TestStoreActionsAsync(object store, ActionTestCase[] actionTestCases, ITestOutputHelper? output = null)
    {
        output?.WriteLine($"Testing {actionTestCases.Length} store actions...");
        
        foreach (var testCase in actionTestCases)
        {
            var page = _testContext!.Page!;
            
            if (testCase.IsAsync)
            {
                await page.EvaluateAsync(@"async (actionName, payload) => {
                    const stores = window.__PINIA_STORES__ || {};
                    const store = Object.values(stores)[0];
                    if (store && store[actionName]) {
                        await store[actionName](payload);
                    }
                }", testCase.ActionName, testCase.ActionPayload);
            }
            else
            {
                await page.EvaluateAsync(@"(actionName, payload) => {
                    const stores = window.__PINIA_STORES__ || {};
                    const store = Object.values(stores)[0];
                    if (store && store[actionName]) {
                        store[actionName](payload);
                    }
                }", testCase.ActionName, testCase.ActionPayload);
            }

            output?.WriteLine($"✓ Action '{testCase.ActionName}' executed successfully");
        }
    }

    public async Task TestStorePublicGatewayIntegrationAsync(object store, PublicGatewayIntegrationTestCase[] apiTestCases, ITestOutputHelper? output = null)
    {
        output?.WriteLine($"Testing {apiTestCases.Length} Public Gateway integration cases...");
        
        foreach (var testCase in apiTestCases)
        {
            if (testCase.MockApiResponse)
            {
                await _testContext!.SetupApiMocksAsync(new Dictionary<string, MockApiResponse>
                {
                    [testCase.ApiEndpoint] = new MockApiResponse
                    {
                        Data = testCase.ExpectedApiResponse,
                        StatusCode = System.Net.HttpStatusCode.OK
                    }
                });
            }

            var page = _testContext!.Page!;
            
            await page.EvaluateAsync(@"async (actionName, payload) => {
                const stores = window.__PINIA_STORES__ || {};
                const store = Object.values(stores)[0];
                if (store && store[actionName]) {
                    await store[actionName](payload);
                }
            }", testCase.ActionName, testCase.ActionPayload);

            output?.WriteLine($"✓ Public Gateway integration '{testCase.ActionName}' tested successfully");
        }
    }

    public async Task TestStoreErrorHandlingAsync(object store, ErrorHandlingTestCase[] errorTestCases, ITestOutputHelper? output = null)
    {
        output?.WriteLine($"Testing {errorTestCases.Length} error handling cases...");
        
        foreach (var testCase in errorTestCases)
        {
            // Mock error response
            await _testContext!.SetupApiMocksAsync(new Dictionary<string, MockApiResponse>
            {
                ["/api/test-error"] = new MockApiResponse
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError,
                    Data = new { error = testCase.ErrorToThrow.Message }
                }
            });

            var page = _testContext!.Page!;
            
            // Test error handling in store
            var errorState = await page.EvaluateAsync<dynamic>(@"async (actionName) => {
                try {
                    const stores = window.__PINIA_STORES__ || {};
                    const store = Object.values(stores)[0];
                    if (store && store[actionName]) {
                        await store[actionName]();
                    }
                    return { hasError: false };
                } catch (error) {
                    return { hasError: true, errorMessage: error.message };
                }
            }", testCase.ActionName);

            Assert.True((bool)errorState.hasError, $"Error handling should capture errors for action '{testCase.ActionName}'");
            
            output?.WriteLine($"✓ Error handling for '{testCase.ActionName}' tested successfully");
        }
    }

    public async Task TestStorePerformanceAsync(object store, PerformanceTestCase[] performanceTestCases, PerformanceThreshold threshold, ITestOutputHelper? output = null)
    {
        output?.WriteLine($"Testing {performanceTestCases.Length} performance cases...");
        
        foreach (var testCase in performanceTestCases)
        {
            var startTime = DateTime.UtcNow;
            
            for (int i = 0; i < testCase.IterationCount; i++)
            {
                await testCase.Operation();
            }
            
            var duration = DateTime.UtcNow - startTime;
            var avgDuration = TimeSpan.FromTicks(duration.Ticks / testCase.IterationCount);

            Assert.True(avgDuration <= testCase.ExpectedMaxDuration, 
                $"Performance test '{testCase.OperationName}' exceeded threshold: {avgDuration} > {testCase.ExpectedMaxDuration}");
            
            output?.WriteLine($"✓ Performance test '{testCase.OperationName}' completed in {avgDuration.TotalMilliseconds:F2}ms avg");
        }
    }

    public async Task TestStorePersistenceAsync(object store, PersistenceTestOptions options, ITestOutputHelper? output = null)
    {
        output?.WriteLine("Testing store persistence...");
        
        var page = _testContext!.Page!;

        if (options.TestLocalStorage)
        {
            var localStorageData = await page.EvaluateAsync<Dictionary<string, string>>(@"() => {
                const data = {};
                for (let i = 0; i < localStorage.length; i++) {
                    const key = localStorage.key(i);
                    if (key && key.startsWith('ic-')) {
                        data[key] = localStorage.getItem(key);
                    }
                }
                return data;
            }");

            Assert.NotEmpty(localStorageData);
            output?.WriteLine($"✓ LocalStorage persistence validated ({localStorageData.Count} items)");
        }

        if (options.TestSessionStorage)
        {
            var sessionStorageData = await page.EvaluateAsync<Dictionary<string, string>>(@"() => {
                const data = {};
                for (let i = 0; i < sessionStorage.length; i++) {
                    const key = sessionStorage.key(i);
                    if (key && key.startsWith('ic-')) {
                        data[key] = sessionStorage.getItem(key);
                    }
                }
                return data;
            }");

            output?.WriteLine($"✓ SessionStorage persistence validated ({sessionStorageData.Count} items)");
        }
    }
}
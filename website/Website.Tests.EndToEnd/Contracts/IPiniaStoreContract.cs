using InternationalCenter.Shared.Tests.Abstractions;
using Xunit.Abstractions;

namespace InternationalCenter.Website.Shared.Tests.Contracts;

/// <summary>
/// Contract for testing Pinia stores in the International Center website
/// Defines standardized tests for state management and Public Gateway integration
/// Medical-grade store testing ensuring data consistency and audit compliance
/// </summary>
/// <typeparam name="TStore">The Pinia store type being tested</typeparam>
public interface IPiniaStoreContract<TStore>
    where TStore : class
{
    /// <summary>
    /// Tests store initialization and default state
    /// Contract: Must validate initial state setup and type safety
    /// </summary>
    Task TestStoreInitializationAsync(
        TStore store,
        StoreInitializationExpectations expectations,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests store state mutations and reactivity
    /// Contract: Must validate state changes trigger reactive updates
    /// </summary>
    Task TestStoreStateMutationsAsync(
        TStore store,
        StateMutationTestCase[] mutationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests store getters and computed values
    /// Contract: Must validate getter computation and dependency tracking
    /// </summary>
    Task TestStoreGettersAsync(
        TStore store,
        GetterTestCase[] getterTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests store actions and async operations
    /// Contract: Must validate action execution and error handling
    /// </summary>
    Task TestStoreActionsAsync(
        TStore store,
        ActionTestCase[] actionTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests store API integration and HTTP requests
    /// Contract: Must validate Public Gateway integration and response handling
    /// </summary>
    Task TestStoreApiIntegrationAsync(
        TStore store,
        ApiIntegrationTestCase[] apiTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests store error handling and recovery
    /// Contract: Must validate error state management and user-friendly error messages
    /// </summary>
    Task TestStoreErrorHandlingAsync(
        TStore store,
        ErrorHandlingTestCase[] errorTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests store performance characteristics
    /// Contract: Must validate store operations meet frontend performance thresholds
    /// </summary>
    Task TestStorePerformanceAsync(
        TStore store,
        PerformanceTestCase[] performanceTestCases,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests store persistence and hydration
    /// Contract: Must validate state persistence across browser sessions
    /// </summary>
    Task TestStorePersistenceAsync(
        TStore store,
        PersistenceTestOptions options,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing Services store
/// Specialized contract for service data management and Public Gateway integration
/// </summary>
/// <typeparam name="TServicesStore">The services store type being tested</typeparam>
public interface IServicesStoreContract<TServicesStore> : IPiniaStoreContract<TServicesStore>
    where TServicesStore : class
{
    /// <summary>
    /// Tests service fetching and caching
    /// Contract: Must validate service data fetching with proper caching strategy
    /// </summary>
    Task TestServiceFetchingAsync(
        TServicesStore store,
        ServiceFetchTestCase[] fetchTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service search functionality
    /// Contract: Must validate search operations with debouncing and result caching
    /// </summary>
    Task TestServiceSearchAsync(
        TServicesStore store,
        ServiceSearchTestCase[] searchTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service filtering and sorting
    /// Contract: Must validate filter application and sort operations
    /// </summary>
    Task TestServiceFilteringAsync(
        TServicesStore store,
        ServiceFilterTestCase[] filterTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service pagination handling
    /// Contract: Must validate pagination state management and page navigation
    /// </summary>
    Task TestServicePaginationAsync(
        TServicesStore store,
        ServicePaginationTestCase[] paginationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service details retrieval
    /// Contract: Must validate individual service detail fetching and caching
    /// </summary>
    Task TestServiceDetailsAsync(
        TServicesStore store,
        ServiceDetailsTestCase[] detailTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service favorites/bookmarks functionality
    /// Contract: Must validate favorite service persistence and management
    /// </summary>
    Task TestServiceFavoritesAsync(
        TServicesStore store,
        ServiceFavoriteTestCase[] favoriteTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing Search store
/// Specialized contract for search state management and query handling
/// </summary>
/// <typeparam name="TSearchStore">The search store type being tested</typeparam>
public interface ISearchStoreContract<TSearchStore> : IPiniaStoreContract<TSearchStore>
    where TSearchStore : class
{
    /// <summary>
    /// Tests search query management
    /// Contract: Must validate search query state and history management
    /// </summary>
    Task TestSearchQueryManagementAsync(
        TSearchStore store,
        SearchQueryTestCase[] queryTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests search filter state management
    /// Contract: Must validate filter state persistence and combination logic
    /// </summary>
    Task TestSearchFilterStateAsync(
        TSearchStore store,
        SearchFilterStateTestCase[] filterStateTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests search result management
    /// Contract: Must validate search result state and update handling
    /// </summary>
    Task TestSearchResultManagementAsync(
        TSearchStore store,
        SearchResultTestCase[] resultTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests search suggestion and autocomplete
    /// Contract: Must validate search suggestions with proper debouncing
    /// </summary>
    Task TestSearchSuggestionsAsync(
        TSearchStore store,
        SearchSuggestionTestCase[] suggestionTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests search history and recent searches
    /// Contract: Must validate search history persistence and management
    /// </summary>
    Task TestSearchHistoryAsync(
        TSearchStore store,
        SearchHistoryTestCase[] historyTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests search geolocation and proximity
    /// Contract: Must validate location-based search functionality
    /// </summary>
    Task TestSearchGeolocationAsync(
        TSearchStore store,
        GeolocationTestCase[] geolocationTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing Categories store
/// Specialized contract for category hierarchy and filtering
/// </summary>
/// <typeparam name="TCategoriesStore">The categories store type being tested</typeparam>
public interface ICategoriesStoreContract<TCategoriesStore> : IPiniaStoreContract<TCategoriesStore>
    where TCategoriesStore : class
{
    /// <summary>
    /// Tests category hierarchy loading and management
    /// Contract: Must validate category tree structure and parent-child relationships
    /// </summary>
    Task TestCategoryHierarchyAsync(
        TCategoriesStore store,
        CategoryHierarchyTestCase[] hierarchyTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category selection and multi-selection
    /// Contract: Must validate category selection state management
    /// </summary>
    Task TestCategorySelectionAsync(
        TCategoriesStore store,
        CategorySelectionTestCase[] selectionTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category filtering and search
    /// Contract: Must validate category search within hierarchy
    /// </summary>
    Task TestCategoryFilteringAsync(
        TCategoriesStore store,
        CategoryFilteringTestCase[] filteringTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category breadcrumb management
    /// Contract: Must validate breadcrumb generation and navigation
    /// </summary>
    Task TestCategoryBreadcrumbsAsync(
        TCategoriesStore store,
        CategoryBreadcrumbTestCase[] breadcrumbTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category expansion and collapse state
    /// Contract: Must validate category tree expansion state persistence
    /// </summary>
    Task TestCategoryExpansionStateAsync(
        TCategoriesStore store,
        CategoryExpansionTestCase[] expansionTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing UI store
/// Specialized contract for UI state management
/// </summary>
/// <typeparam name="TUIStore">The UI store type being tested</typeparam>
public interface IUIStoreContract<TUIStore> : IPiniaStoreContract<TUIStore>
    where TUIStore : class
{
    /// <summary>
    /// Tests loading state management
    /// Contract: Must validate loading indicators and state tracking
    /// </summary>
    Task TestLoadingStateManagementAsync(
        TUIStore store,
        LoadingStateTestCase[] loadingTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests modal and dialog management
    /// Contract: Must validate modal state and stack management
    /// </summary>
    Task TestModalManagementAsync(
        TUIStore store,
        ModalTestCase[] modalTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests notification and toast management
    /// Contract: Must validate notification queue and display logic
    /// </summary>
    Task TestNotificationManagementAsync(
        TUIStore store,
        NotificationTestCase[] notificationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests theme and appearance settings
    /// Contract: Must validate theme persistence and application
    /// </summary>
    Task TestThemeManagementAsync(
        TUIStore store,
        ThemeTestCase[] themeTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests responsive breakpoint management
    /// Contract: Must validate breakpoint detection and state updates
    /// </summary>
    Task TestBreakpointManagementAsync(
        TUIStore store,
        BreakpointTestCase[] breakpointTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests accessibility preferences
    /// Contract: Must validate accessibility setting persistence and application
    /// </summary>
    Task TestAccessibilityPreferencesAsync(
        TUIStore store,
        AccessibilityPreferenceTestCase[] accessibilityTestCases,
        ITestOutputHelper? output = null);
}

// Supporting classes for store testing

/// <summary>
/// Expectations for store initialization
/// </summary>
public class StoreInitializationExpectations
{
    public Dictionary<string, object>? ExpectedInitialState { get; set; }
    public string[]? RequiredProperties { get; set; }
    public Type[]? RequiredGetterTypes { get; set; }
    public string[]? RequiredActions { get; set; }
    public bool ShouldBePersisted { get; set; } = false;
}

/// <summary>
/// Test case for state mutations
/// </summary>
public class StateMutationTestCase
{
    public string ActionName { get; set; } = string.Empty;
    public object? ActionPayload { get; set; }
    public Dictionary<string, object>? ExpectedStateChanges { get; set; }
    public bool ShouldTriggerReactivity { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>
/// Test case for getters
/// </summary>
public class GetterTestCase
{
    public string GetterName { get; set; } = string.Empty;
    public object? ExpectedValue { get; set; }
    public Dictionary<string, object>? StatePrerequisites { get; set; }
    public bool TestReactivity { get; set; } = true;
    public Func<object?, bool>? ValueValidator { get; set; }
}

/// <summary>
/// Test case for actions
/// </summary>
public class ActionTestCase
{
    public string ActionName { get; set; } = string.Empty;
    public object? ActionPayload { get; set; }
    public bool IsAsync { get; set; } = false;
    public Dictionary<string, object>? ExpectedStateChanges { get; set; }
    public Exception? ExpectedException { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Test case for API integration
/// </summary>
public class ApiIntegrationTestCase
{
    public string ActionName { get; set; } = string.Empty;
    public object? ActionPayload { get; set; }
    public string ApiEndpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public object? ExpectedApiResponse { get; set; }
    public Dictionary<string, object>? ExpectedStateChanges { get; set; }
    public bool MockApiResponse { get; set; } = true;
    public TimeSpan ApiTimeout { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Test case for error handling
/// </summary>
public class ErrorHandlingTestCase
{
    public string ActionName { get; set; } = string.Empty;
    public Exception ErrorToThrow { get; set; } = new("Test error");
    public string ExpectedErrorState { get; set; } = string.Empty;
    public string? ExpectedUserMessage { get; set; }
    public bool ShouldRecover { get; set; } = true;
    public TimeSpan RecoveryTimeout { get; set; } = TimeSpan.FromSeconds(3);
}

/// <summary>
/// Test case for performance
/// </summary>
public class PerformanceTestCase
{
    public string OperationName { get; set; } = string.Empty;
    public Func<Task> Operation { get; set; } = () => Task.CompletedTask;
    public int IterationCount { get; set; } = 100;
    public TimeSpan ExpectedMaxDuration { get; set; } = TimeSpan.FromMilliseconds(50);
    public long ExpectedMaxMemory { get; set; } = 10 * 1024 * 1024; // 10MB
}

/// <summary>
/// Test case for service fetching
/// </summary>
public class ServiceFetchTestCase
{
    public string FetchAction { get; set; } = string.Empty;
    public object? FetchParameters { get; set; }
    public ServiceTestData[] ExpectedServices { get; set; } = Array.Empty<ServiceTestData>();
    public bool ShouldUseCache { get; set; } = true;
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Test case for service search
/// </summary>
public class ServiceSearchTestCase
{
    public string SearchQuery { get; set; } = string.Empty;
    public Dictionary<string, object>? SearchFilters { get; set; }
    public int ExpectedResultCount { get; set; }
    public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromMilliseconds(300);
    public bool TestCaching { get; set; } = true;
}

/// <summary>
/// Test case for service filtering
/// </summary>
public class ServiceFilterTestCase
{
    public string FilterName { get; set; } = string.Empty;
    public object FilterValue { get; set; } = new();
    public bool ShouldCombineWithExistingFilters { get; set; } = true;
    public int ExpectedResultCount { get; set; }
    public TimeSpan FilterDelay { get; set; } = TimeSpan.FromMilliseconds(100);
}

/// <summary>
/// Test case for service pagination
/// </summary>
public class ServicePaginationTestCase
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; } = 100;
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool TestPageNavigation { get; set; } = true;
}

/// <summary>
/// Test case for service details
/// </summary>
public class ServiceDetailsTestCase
{
    public Guid ServiceId { get; set; } = Guid.NewGuid();
    public ServiceTestData ExpectedServiceDetails { get; set; } = new();
    public bool ShouldUseCache { get; set; } = true;
    public bool TestNotFound { get; set; } = false;
}

/// <summary>
/// Test case for service favorites
/// </summary>
public class ServiceFavoriteTestCase
{
    public Guid ServiceId { get; set; } = Guid.NewGuid();
    public bool IsFavorite { get; set; } = true;
    public bool TestPersistence { get; set; } = true;
    public bool TestLimitReached { get; set; } = false;
    public int MaxFavorites { get; set; } = 50;
}

/// <summary>
/// Test case for search queries
/// </summary>
public class SearchQueryTestCase
{
    public string Query { get; set; } = string.Empty;
    public bool ShouldSaveToHistory { get; set; } = true;
    public bool ShouldTriggerSuggestions { get; set; } = true;
    public int MinQueryLength { get; set; } = 2;
    public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromMilliseconds(300);
}

/// <summary>
/// Test case for search filter state
/// </summary>
public class SearchFilterStateTestCase
{
    public Dictionary<string, object> Filters { get; set; } = new();
    public bool ShouldPersist { get; set; } = true;
    public bool ShouldCombineFilters { get; set; } = true;
    public string? ExpectedUrlParams { get; set; }
}

/// <summary>
/// Test case for search results
/// </summary>
public class SearchResultTestCase
{
    public string SearchQuery { get; set; } = string.Empty;
    public SearchResultTestData ExpectedResults { get; set; } = new();
    public bool TestResultSorting { get; set; } = true;
    public bool TestResultGrouping { get; set; } = false;
    public TimeSpan ResultTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Test case for search suggestions
/// </summary>
public class SearchSuggestionTestCase
{
    public string PartialQuery { get; set; } = string.Empty;
    public string[] ExpectedSuggestions { get; set; } = Array.Empty<string>();
    public int MaxSuggestions { get; set; } = 10;
    public TimeSpan SuggestionDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}

/// <summary>
/// Test case for search history
/// </summary>
public class SearchHistoryTestCase
{
    public string[] Queries { get; set; } = Array.Empty<string>();
    public int MaxHistorySize { get; set; } = 20;
    public bool TestClearHistory { get; set; } = true;
    public bool TestHistoryPersistence { get; set; } = true;
}

/// <summary>
/// Test case for geolocation
/// </summary>
public class GeolocationTestCase
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; } = 10; // km
    public bool TestLocationPermission { get; set; } = true;
    public bool TestLocationAccuracy { get; set; } = false;
}

/// <summary>
/// Test case for category hierarchy
/// </summary>
public class CategoryHierarchyTestCase
{
    public CategoryTestData[] Categories { get; set; } = Array.Empty<CategoryTestData>();
    public int MaxDepth { get; set; } = 5;
    public bool TestLazyLoading { get; set; } = false;
    public bool TestCaching { get; set; } = true;
}

/// <summary>
/// Test case for category filtering
/// </summary>
public class CategoryFilteringTestCase
{
    public string FilterQuery { get; set; } = string.Empty;
    public bool IncludeChildren { get; set; } = true;
    public bool MatchDescription { get; set; } = false;
    public int ExpectedMatchCount { get; set; }
}

/// <summary>
/// Test case for category breadcrumbs
/// </summary>
public class CategoryBreadcrumbTestCase
{
    public Guid SelectedCategoryId { get; set; } = Guid.NewGuid();
    public string[] ExpectedBreadcrumbs { get; set; } = Array.Empty<string>();
    public bool TestNavigation { get; set; } = true;
}

/// <summary>
/// Test case for category expansion
/// </summary>
public class CategoryExpansionTestCase
{
    public Guid CategoryId { get; set; } = Guid.NewGuid();
    public bool IsExpanded { get; set; } = true;
    public bool TestPersistence { get; set; } = true;
    public bool TestChildrenLoading { get; set; } = false;
}

/// <summary>
/// Test case for loading states
/// </summary>
public class LoadingStateTestCase
{
    public string LoadingKey { get; set; } = string.Empty;
    public bool IsLoading { get; set; } = true;
    public string? LoadingMessage { get; set; }
    public TimeSpan LoadingDuration { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>
/// Test case for modals
/// </summary>
public class ModalTestCase
{
    public string ModalId { get; set; } = string.Empty;
    public bool IsOpen { get; set; } = true;
    public object? ModalData { get; set; }
    public bool TestModalStack { get; set; } = true;
    public bool TestBackdropClick { get; set; } = true;
}

/// <summary>
/// Test case for notifications
/// </summary>
public class NotificationTestCase
{
    public string NotificationType { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public TimeSpan? AutoDismiss { get; set; } = TimeSpan.FromSeconds(5);
    public bool TestQueue { get; set; } = true;
    public int MaxNotifications { get; set; } = 5;
}

/// <summary>
/// Test case for theme
/// </summary>
public class ThemeTestCase
{
    public string ThemeName { get; set; } = "light";
    public bool TestPersistence { get; set; } = true;
    public bool TestSystemTheme { get; set; } = true;
    public Dictionary<string, string>? ExpectedCssVariables { get; set; }
}

/// <summary>
/// Test case for breakpoints
/// </summary>
public class BreakpointTestCase
{
    public int ViewportWidth { get; set; } = 1024;
    public int ViewportHeight { get; set; } = 768;
    public string ExpectedBreakpoint { get; set; } = "md";
    public bool TestBreakpointChange { get; set; } = true;
}

/// <summary>
/// Test case for accessibility preferences
/// </summary>
public class AccessibilityPreferenceTestCase
{
    public bool ReduceMotion { get; set; } = false;
    public bool HighContrast { get; set; } = false;
    public int FontSize { get; set; } = 100; // percentage
    public bool ScreenReaderMode { get; set; } = false;
    public bool TestPersistence { get; set; } = true;
}
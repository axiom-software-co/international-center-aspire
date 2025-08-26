using InternationalCenter.Shared.Tests.Abstractions;
using Xunit.Abstractions;

namespace InternationalCenter.Website.Shared.Tests.Contracts;

/// <summary>
/// Contract for testing Vue components in the International Center website
/// Defines standardized tests for Vue component behavior and Public Gateway integration
/// Medical-grade component testing ensuring accessibility and performance compliance
/// </summary>
/// <typeparam name="TComponent">The Vue component type being tested</typeparam>
public interface IComponentContract<TComponent>
    where TComponent : class
{
    /// <summary>
    /// Tests component initialization and default state
    /// Contract: Must validate component mounts properly with default props and state
    /// </summary>
    Task TestComponentInitializationAsync(
        TComponent component,
        ComponentTestOptions? options = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests component props validation and reactivity
    /// Contract: Must validate all prop types, required props, and reactive updates
    /// </summary>
    Task TestComponentPropsAsync(
        TComponent component,
        Dictionary<string, PropTestCase> propTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests component event emission and custom event handling
    /// Contract: Must validate event emission with correct payload and timing
    /// </summary>
    Task TestComponentEventsAsync(
        TComponent component,
        Dictionary<string, EventTestCase> eventTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests component accessibility compliance
    /// Contract: Must validate WCAG 2.1 AA compliance and keyboard navigation
    /// </summary>
    Task TestComponentAccessibilityAsync(
        TComponent component,
        AccessibilityTestOptions? options = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests component performance characteristics
    /// Contract: Must validate component render time meets frontend performance thresholds
    /// </summary>
    Task TestComponentPerformanceAsync(
        TComponent component,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests component responsive design across viewport sizes
    /// Contract: Must validate component layout and usability across different screen sizes
    /// </summary>
    Task TestComponentResponsivenessAsync(
        TComponent component,
        ViewportSize[] viewportSizes,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests component error handling and error boundaries
    /// Contract: Must validate graceful error handling and user-friendly error states
    /// </summary>
    Task TestComponentErrorHandlingAsync(
        TComponent component,
        ComponentErrorTestCase[] errorTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing Service-related Vue components
/// Specialized contract for components that display and interact with service data
/// </summary>
/// <typeparam name="TServiceComponent">The service component type being tested</typeparam>
public interface IServiceComponentContract<TServiceComponent> : IComponentContract<TServiceComponent>
    where TServiceComponent : class
{
    /// <summary>
    /// Tests service data display and formatting
    /// Contract: Must validate proper service information display with data sanitization
    /// </summary>
    Task TestServiceDataDisplayAsync(
        TServiceComponent component,
        ServiceTestData serviceData,
        DataDisplayValidationRules? rules = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service search and filtering functionality
    /// Contract: Must validate search input handling and result filtering
    /// </summary>
    Task TestServiceSearchFilteringAsync(
        TServiceComponent component,
        SearchFilterTestCase[] searchTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service category integration
    /// Contract: Must validate category display and filtering with proper hierarchy support
    /// </summary>
    Task TestServiceCategoryIntegrationAsync(
        TServiceComponent component,
        CategoryTestData[] categoryData,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service contact information display
    /// Contract: Must validate contact information display with privacy protection
    /// </summary>
    Task TestServiceContactDisplayAsync(
        TServiceComponent component,
        ContactInformationTestData contactData,
        PrivacyValidationRules? privacyRules = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service location and mapping integration
    /// Contract: Must validate address display and map integration functionality
    /// </summary>
    Task TestServiceLocationIntegrationAsync(
        TServiceComponent component,
        LocationTestData locationData,
        MapIntegrationOptions? mapOptions = null,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing Search-related Vue components
/// Specialized contract for components that handle search functionality
/// </summary>
/// <typeparam name="TSearchComponent">The search component type being tested</typeparam>
public interface ISearchComponentContract<TSearchComponent> : IComponentContract<TSearchComponent>
    where TSearchComponent : class
{
    /// <summary>
    /// Tests search input validation and sanitization
    /// Contract: Must validate input sanitization and XSS prevention
    /// </summary>
    Task TestSearchInputValidationAsync(
        TSearchComponent component,
        SearchInputTestCase[] inputTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests search result display and pagination
    /// Contract: Must validate search results display with proper pagination controls
    /// </summary>
    Task TestSearchResultDisplayAsync(
        TSearchComponent component,
        SearchResultTestData searchResults,
        PaginationTestOptions? paginationOptions = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests search filter application and removal
    /// Contract: Must validate filter application with proper state management
    /// </summary>
    Task TestSearchFilterManagementAsync(
        TSearchComponent component,
        FilterTestCase[] filterTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests search performance and debouncing
    /// Contract: Must validate search input debouncing and API call optimization
    /// </summary>
    Task TestSearchPerformanceAsync(
        TSearchComponent component,
        SearchPerformanceTestOptions options,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests search accessibility and keyboard navigation
    /// Contract: Must validate screen reader compatibility and keyboard shortcuts
    /// </summary>
    Task TestSearchAccessibilityAsync(
        TSearchComponent component,
        SearchAccessibilityOptions? options = null,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing Category-related Vue components
/// Specialized contract for components that handle category display and navigation
/// </summary>
/// <typeparam name="TCategoryComponent">The category component type being tested</typeparam>
public interface ICategoryComponentContract<TCategoryComponent> : IComponentContract<TCategoryComponent>
    where TCategoryComponent : class
{
    /// <summary>
    /// Tests category hierarchy display and navigation
    /// Contract: Must validate proper category tree display with expandable/collapsible nodes
    /// </summary>
    Task TestCategoryHierarchyDisplayAsync(
        TCategoryComponent component,
        CategoryHierarchyTestData hierarchyData,
        HierarchyDisplayOptions? options = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category selection and multi-selection
    /// Contract: Must validate category selection state management
    /// </summary>
    Task TestCategorySelectionAsync(
        TCategoryComponent component,
        CategorySelectionTestCase[] selectionTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category filtering and search
    /// Contract: Must validate category search within hierarchy structure
    /// </summary>
    Task TestCategoryFilteringAsync(
        TCategoryComponent component,
        CategoryFilterTestCase[] filterTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category breadcrumb navigation
    /// Contract: Must validate breadcrumb generation and navigation functionality
    /// </summary>
    Task TestCategoryBreadcrumbsAsync(
        TCategoryComponent component,
        BreadcrumbTestData[] breadcrumbData,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing UI-related Vue components
/// Specialized contract for general UI components like modals, buttons, forms
/// </summary>
/// <typeparam name="TUIComponent">The UI component type being tested</typeparam>
public interface IUIComponentContract<TUIComponent> : IComponentContract<TUIComponent>
    where TUIComponent : class
{
    /// <summary>
    /// Tests UI component state management
    /// Contract: Must validate component state transitions and visual feedback
    /// </summary>
    Task TestUIStateManagementAsync(
        TUIComponent component,
        UIStateTestCase[] stateTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests UI component animation and transitions
    /// Contract: Must validate smooth animations and proper transition timing
    /// </summary>
    Task TestUIAnimationsAsync(
        TUIComponent component,
        AnimationTestOptions? options = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests UI component form validation
    /// Contract: Must validate form input validation and error message display
    /// </summary>
    Task TestUIFormValidationAsync(
        TUIComponent component,
        FormValidationTestCase[] validationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests UI component theme and styling
    /// Contract: Must validate component styling consistency and theme support
    /// </summary>
    Task TestUIThemeAndStylingAsync(
        TUIComponent component,
        ThemeTestOptions? themeOptions = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests UI component loading states
    /// Contract: Must validate loading indicators and skeleton screens
    /// </summary>
    Task TestUILoadingStatesAsync(
        TUIComponent component,
        LoadingStateTestCase[] loadingTestCases,
        ITestOutputHelper? output = null);
}

// Supporting classes and enums for component testing

/// <summary>
/// Options for component testing
/// </summary>
public class ComponentTestOptions
{
    public bool MountWithRouter { get; set; } = false;
    public bool MountWithStore { get; set; } = false;
    public Dictionary<string, object>? GlobalProperties { get; set; }
    public Dictionary<string, object>? Provide { get; set; }
    public object[]? Plugins { get; set; }
}

/// <summary>
/// Test case for component props
/// </summary>
public class PropTestCase
{
    public object? Value { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ExpectedError { get; set; }
    public bool TestReactivity { get; set; } = true;
}

/// <summary>
/// Test case for component events
/// </summary>
public class EventTestCase
{
    public string EventName { get; set; } = string.Empty;
    public string TriggerAction { get; set; } = string.Empty;
    public object? ExpectedPayload { get; set; }
    public int ExpectedCallCount { get; set; } = 1;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>
/// Options for accessibility testing
/// </summary>
public class AccessibilityTestOptions
{
    public bool ValidateAriaLabels { get; set; } = true;
    public bool ValidateKeyboardNavigation { get; set; } = true;
    public bool ValidateColorContrast { get; set; } = true;
    public bool ValidateSemanticHTML { get; set; } = true;
    public string[] IgnoreRules { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Test case for component errors
/// </summary>
public class ComponentErrorTestCase
{
    public string ErrorName { get; set; } = string.Empty;
    public Exception ErrorToThrow { get; set; } = new("Test error");
    public string ExpectedErrorMessage { get; set; } = string.Empty;
    public bool ShouldRecover { get; set; } = true;
}

/// <summary>
/// Test data for services
/// </summary>
public class ServiceTestData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public ContactInformationTestData? ContactInformation { get; set; }
    public LocationTestData? Location { get; set; }
    public string[]? Categories { get; set; }
}

/// <summary>
/// Validation rules for data display
/// </summary>
public class DataDisplayValidationRules
{
    public bool ValidateHtmlSanitization { get; set; } = true;
    public bool ValidateTextTruncation { get; set; } = true;
    public bool ValidateNullValueHandling { get; set; } = true;
    public bool ValidateDateFormatting { get; set; } = true;
}

/// <summary>
/// Test case for search and filtering
/// </summary>
public class SearchFilterTestCase
{
    public string SearchTerm { get; set; } = string.Empty;
    public Dictionary<string, object>? Filters { get; set; }
    public int ExpectedResultCount { get; set; }
    public bool ShouldTriggerApiCall { get; set; } = true;
}

/// <summary>
/// Test data for categories
/// </summary>
public class CategoryTestData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int Level { get; set; }
    public CategoryTestData[]? Children { get; set; }
}

/// <summary>
/// Test data for contact information
/// </summary>
public class ContactInformationTestData
{
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Fax { get; set; }
}

/// <summary>
/// Privacy validation rules
/// </summary>
public class PrivacyValidationRules
{
    public bool MaskPhoneNumbers { get; set; } = false;
    public bool ValidateEmailDisplay { get; set; } = true;
    public bool ValidateWebsiteLinks { get; set; } = true;
}

/// <summary>
/// Test data for location
/// </summary>
public class LocationTestData
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
/// Options for map integration testing
/// </summary>
public class MapIntegrationOptions
{
    public bool TestMapDisplay { get; set; } = true;
    public bool TestMarkerPlacement { get; set; } = true;
    public bool TestZoomControls { get; set; } = true;
    public bool TestDirections { get; set; } = false;
}

/// <summary>
/// Test case for search input validation
/// </summary>
public class SearchInputTestCase
{
    public string Input { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
    public string? ExpectedError { get; set; }
    public bool ShouldTriggerSearch { get; set; } = true;
}

/// <summary>
/// Test data for search results
/// </summary>
public class SearchResultTestData
{
    public ServiceTestData[] Results { get; set; } = Array.Empty<ServiceTestData>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Options for pagination testing
/// </summary>
public class PaginationTestOptions
{
    public bool TestPageNavigation { get; set; } = true;
    public bool TestPageSizeChange { get; set; } = true;
    public bool TestFirstLastButtons { get; set; } = true;
    public bool TestKeyboardNavigation { get; set; } = true;
}

/// <summary>
/// Test case for filters
/// </summary>
public class FilterTestCase
{
    public string FilterName { get; set; } = string.Empty;
    public object FilterValue { get; set; } = new();
    public bool ShouldApply { get; set; } = true;
    public int ExpectedResultCount { get; set; }
}

/// <summary>
/// Options for search performance testing
/// </summary>
public class SearchPerformanceTestOptions
{
    public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromMilliseconds(300);
    public int MinSearchLength { get; set; } = 2;
    public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromSeconds(2);
    public bool TestCaching { get; set; } = true;
}

/// <summary>
/// Options for search accessibility
/// </summary>
public class SearchAccessibilityOptions
{
    public bool TestScreenReaderAnnouncements { get; set; } = true;
    public bool TestKeyboardShortcuts { get; set; } = true;
    public bool TestAriaLiveRegions { get; set; } = true;
    public bool TestFocusManagement { get; set; } = true;
}

/// <summary>
/// Test data for category hierarchy
/// </summary>
public class CategoryHierarchyTestData
{
    public CategoryTestData[] RootCategories { get; set; } = Array.Empty<CategoryTestData>();
    public int MaxDepth { get; set; } = 5;
    public int MaxBreadth { get; set; } = 10;
}

/// <summary>
/// Options for hierarchy display
/// </summary>
public class HierarchyDisplayOptions
{
    public bool TestExpansion { get; set; } = true;
    public bool TestCollapse { get; set; } = true;
    public bool TestLazyLoading { get; set; } = false;
    public bool TestVirtualScrolling { get; set; } = false;
}

/// <summary>
/// Test case for category selection
/// </summary>
public class CategorySelectionTestCase
{
    public Guid[] SelectedCategoryIds { get; set; } = Array.Empty<Guid>();
    public bool MultiSelect { get; set; } = false;
    public bool AllowParentSelection { get; set; } = true;
    public int ExpectedSelectionCount { get; set; }
}

/// <summary>
/// Test case for category filtering
/// </summary>
public class CategoryFilterTestCase
{
    public string FilterText { get; set; } = string.Empty;
    public bool IncludeChildren { get; set; } = true;
    public bool MatchDescription { get; set; } = false;
    public int ExpectedMatchCount { get; set; }
}

/// <summary>
/// Test data for breadcrumbs
/// </summary>
public class BreadcrumbTestData
{
    public Guid CategoryId { get; set; } = Guid.NewGuid();
    public string[] ExpectedBreadcrumbs { get; set; } = Array.Empty<string>();
    public bool TestNavigation { get; set; } = true;
}

/// <summary>
/// Test case for UI state
/// </summary>
public class UIStateTestCase
{
    public string StateName { get; set; } = string.Empty;
    public string TriggerAction { get; set; } = string.Empty;
    public Dictionary<string, object>? ExpectedState { get; set; }
    public string[]? ExpectedCssClasses { get; set; }
}

/// <summary>
/// Options for animation testing
/// </summary>
public class AnimationTestOptions
{
    public bool TestEnterAnimations { get; set; } = true;
    public bool TestLeaveAnimations { get; set; } = true;
    public bool TestTransitionDuration { get; set; } = true;
    public TimeSpan MaxAnimationDuration { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Test case for form validation
/// </summary>
public class FormValidationTestCase
{
    public string FieldName { get; set; } = string.Empty;
    public object? Value { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ExpectedError { get; set; }
    public bool TestAsyncValidation { get; set; } = false;
}

/// <summary>
/// Options for theme testing
/// </summary>
public class ThemeTestOptions
{
    public string[] ThemesToTest { get; set; } = { "light", "dark" };
    public bool TestColorScheme { get; set; } = true;
    public bool TestTypography { get; set; } = true;
    public bool TestSpacing { get; set; } = true;
}

/// <summary>
/// Test case for loading states
/// </summary>
public class LoadingStateTestCase
{
    public string LoadingStateName { get; set; } = string.Empty;
    public TimeSpan LoadingDuration { get; set; } = TimeSpan.FromSeconds(2);
    public bool ShowSkeleton { get; set; } = true;
    public bool ShowSpinner { get; set; } = false;
    public string? LoadingMessage { get; set; }
}
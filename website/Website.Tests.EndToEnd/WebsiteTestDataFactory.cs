using InternationalCenter.Website.Shared.Tests.Contracts;
using InternationalCenter.Shared.Tests.Abstractions;
using System.Text.Json;

namespace Website.Tests.EndToEnd;

/// <summary>
/// Test data factory for Website end-to-end tests
/// Provides realistic mock data for Public Gateway API responses and component testing
/// Medical-grade test data ensuring anonymous user safety and privacy compliance
/// </summary>
public class WebsiteTestDataFactory : IWebsiteTestDataFactoryContract
{
    private readonly Random _random = new();
    private readonly string[] _serviceNames = {
        "Cardiology Consultation", "Pediatric Care", "Emergency Medicine", "Orthopedic Surgery",
        "Mental Health Services", "Radiology Imaging", "Laboratory Services", "Physical Therapy",
        "Dermatology", "Internal Medicine", "Neurology", "Oncology", "Endocrinology"
    };
    
    private readonly string[] _categoryNames = {
        "Primary Care", "Specialty Care", "Emergency Services", "Diagnostic Services",
        "Surgical Services", "Rehabilitation", "Mental Health", "Preventive Care",
        "Women's Health", "Pediatric Services", "Senior Care"
    };

    private readonly string[] _locations = {
        "Main Hospital Campus", "North Medical Center", "Downtown Clinic", "Suburban Health Center",
        "Community Outreach Center", "Emergency Care Facility", "Specialty Surgery Center"
    };

    public async Task<ServiceTestData[]> CreateMockServiceDataAsync(
        int count = 10, 
        Action<ServiceTestData>? configure = null, 
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Simulate async operation

        var services = new ServiceTestData[count];
        
        for (int i = 0; i < count; i++)
        {
            var service = new ServiceTestData
            {
                Id = Guid.NewGuid(),
                Name = _serviceNames[_random.Next(_serviceNames.Length)],
                Description = GenerateServiceDescription(),
                CategoryId = Guid.NewGuid(),
                CategoryName = _categoryNames[_random.Next(_categoryNames.Length)],
                Location = _locations[_random.Next(_locations.Length)],
                PhoneNumber = GeneratePhoneNumber(),
                Email = GenerateEmail(),
                WebsiteUrl = GenerateWebsiteUrl(),
                IsActive = true,
                AcceptsNewPatients = _random.Next(2) == 0,
                Rating = Math.Round(_random.NextDouble() * 2 + 3, 1), // 3.0 - 5.0 rating
                ReviewCount = _random.Next(50, 500),
                OperatingHours = GenerateOperatingHours(),
                CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(365))
            };

            configure?.Invoke(service);
            services[i] = service;
        }

        return services;
    }

    public async Task<CategoryTestData[]> CreateMockCategoryDataAsync(
        int depth = 3, 
        int breadth = 5, 
        Action<CategoryTestData>? configure = null, 
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var categories = new List<CategoryTestData>();
        var categoryIndex = 0;

        for (int level = 0; level < depth; level++)
        {
            for (int i = 0; i < breadth; i++)
            {
                var category = new CategoryTestData
                {
                    Id = Guid.NewGuid(),
                    Name = $"{_categoryNames[categoryIndex % _categoryNames.Length]} L{level}",
                    Description = $"Healthcare services in {_categoryNames[categoryIndex % _categoryNames.Length].ToLower()}",
                    ParentId = level > 0 ? categories[_random.Next(Math.Min(categories.Count, breadth))].Id : null,
                    Level = level,
                    ServiceCount = _random.Next(5, 50),
                    IsActive = true,
                    SortOrder = i,
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(180))
                };

                configure?.Invoke(category);
                categories.Add(category);
                categoryIndex++;
            }
        }

        return categories.ToArray();
    }

    public async Task<MockApiResponse[]> CreateMockApiResponsesAsync(
        string[] endpoints, 
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var responses = new List<MockApiResponse>();

        foreach (var endpoint in endpoints)
        {
            var response = endpoint.ToLower() switch
            {
                var e when e.Contains("services") => new MockApiResponse
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Data = await CreateMockServiceDataAsync(10, cancellationToken: cancellationToken),
                    Headers = { ["Content-Type"] = "application/json" }
                },
                var e when e.Contains("categories") => new MockApiResponse
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Data = await CreateMockCategoryDataAsync(cancellationToken: cancellationToken),
                    Headers = { ["Content-Type"] = "application/json" }
                },
                var e when e.Contains("search") => new MockApiResponse
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Data = new SearchResultTestData
                    {
                        Query = "test",
                        Results = await CreateMockServiceDataAsync(5, cancellationToken: cancellationToken),
                        TotalCount = 5,
                        Page = 1,
                        PageSize = 20
                    },
                    Headers = { ["Content-Type"] = "application/json" }
                },
                _ => new MockApiResponse
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Data = new { message = "Mock response", endpoint },
                    Headers = { ["Content-Type"] = "application/json" }
                }
            };

            responses.Add(response);
        }

        return responses.ToArray();
    }

    public async Task<UserInteractionSequence[]> CreateMockUserInteractionsAsync(
        string[] workflows, 
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        var interactions = new List<UserInteractionSequence>();

        foreach (var workflow in workflows)
        {
            var sequence = workflow.ToLower() switch
            {
                "service-search" => new UserInteractionSequence
                {
                    WorkflowName = "Service Search",
                    Steps = new[]
                    {
                        new InteractionStep { Action = "navigate", Target = "/services", ExpectedResult = "Services page loads" },
                        new InteractionStep { Action = "type", Target = "search-input", Value = "cardiology", ExpectedResult = "Search suggestions appear" },
                        new InteractionStep { Action = "click", Target = "search-button", ExpectedResult = "Search results display" },
                        new InteractionStep { Action = "click", Target = "service-item:first", ExpectedResult = "Service details page opens" }
                    },
                    ExpectedDuration = TimeSpan.FromSeconds(15)
                },
                "category-navigation" => new UserInteractionSequence
                {
                    WorkflowName = "Category Navigation",
                    Steps = new[]
                    {
                        new InteractionStep { Action = "navigate", Target = "/categories", ExpectedResult = "Categories page loads" },
                        new InteractionStep { Action = "click", Target = "category-item:first", ExpectedResult = "Category expands" },
                        new InteractionStep { Action = "click", Target = "subcategory-item:first", ExpectedResult = "Services filter applied" },
                        new InteractionStep { Action = "verify", Target = "breadcrumbs", ExpectedResult = "Breadcrumb navigation visible" }
                    },
                    ExpectedDuration = TimeSpan.FromSeconds(10)
                },
                _ => new UserInteractionSequence
                {
                    WorkflowName = "Default Workflow",
                    Steps = new[]
                    {
                        new InteractionStep { Action = "navigate", Target = "/", ExpectedResult = "Homepage loads" }
                    },
                    ExpectedDuration = TimeSpan.FromSeconds(5)
                }
            };

            interactions.Add(sequence);
        }

        return interactions.ToArray();
    }

    public async Task ValidateMockDataQualityAsync<T>(
        T mockData, 
        MockDataQualityRules<T>? qualityRules = null) where T : class
    {
        await Task.Delay(1);

        var rules = qualityRules ?? new MockDataQualityRules<T>();
        
        if (!rules.IsRealistic(mockData))
        {
            throw new InvalidOperationException("Mock data does not meet realism requirements");
        }

        if (!rules.MatchesApiSchema(mockData))
        {
            throw new InvalidOperationException("Mock data does not match expected API schema");
        }

        if (!rules.HasRequiredFields(mockData))
        {
            throw new InvalidOperationException("Mock data missing required fields");
        }

        // Check for forbidden patterns in string properties
        var jsonString = JsonSerializer.Serialize(mockData);
        foreach (var pattern in rules.ForbiddenPatterns)
        {
            if (jsonString.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Mock data contains forbidden pattern: {pattern}");
            }
        }
    }

    private string GenerateServiceDescription()
    {
        var descriptions = new[]
        {
            "Comprehensive medical care with experienced healthcare professionals dedicated to patient wellness.",
            "State-of-the-art medical services providing personalized treatment plans for optimal health outcomes.",
            "Expert medical professionals offering advanced healthcare solutions in a patient-centered environment.",
            "Quality healthcare services focused on preventive care and treatment excellence.",
            "Advanced medical care combining cutting-edge technology with compassionate patient care."
        };
        
        return descriptions[_random.Next(descriptions.Length)];
    }

    private string GeneratePhoneNumber()
    {
        return $"({_random.Next(200, 999)}) {_random.Next(200, 999)}-{_random.Next(1000, 9999)}";
    }

    private string GenerateEmail()
    {
        var domains = new[] { "medcenter.com", "healthcare.org", "medicalservices.net" };
        var names = new[] { "info", "appointments", "contact", "services", "care" };
        
        return $"{names[_random.Next(names.Length)]}@{domains[_random.Next(domains.Length)]}";
    }

    private string GenerateWebsiteUrl()
    {
        var domains = new[] { "medicalcenter.com", "healthservices.org", "carecentral.net" };
        return $"https://www.{domains[_random.Next(domains.Length)]}";
    }

    private Dictionary<string, string> GenerateOperatingHours()
    {
        return new Dictionary<string, string>
        {
            ["Monday"] = "8:00 AM - 6:00 PM",
            ["Tuesday"] = "8:00 AM - 6:00 PM",
            ["Wednesday"] = "8:00 AM - 6:00 PM",
            ["Thursday"] = "8:00 AM - 6:00 PM",
            ["Friday"] = "8:00 AM - 5:00 PM",
            ["Saturday"] = _random.Next(2) == 0 ? "9:00 AM - 1:00 PM" : "Closed",
            ["Sunday"] = "Closed"
        };
    }
}

/// <summary>
/// Test data model for services
/// </summary>
public class ServiceTestData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool AcceptsNewPatients { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public Dictionary<string, string> OperatingHours { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Test data model for categories
/// </summary>
public class CategoryTestData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int Level { get; set; }
    public int ServiceCount { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Test data model for search results
/// </summary>
public class SearchResultTestData
{
    public string Query { get; set; } = string.Empty;
    public ServiceTestData[] Results { get; set; } = Array.Empty<ServiceTestData>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Test data model for contact information
/// </summary>
public class ContactInformationTestData
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public Dictionary<string, string> OperatingHours { get; set; } = new();
}

/// <summary>
/// Test data model for locations
/// </summary>
public class LocationTestData
{
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public bool HasParking { get; set; }
    public bool IsAccessible { get; set; }
}

/// <summary>
/// User interaction sequence for workflow testing
/// </summary>
public class UserInteractionSequence
{
    public string WorkflowName { get; set; } = string.Empty;
    public InteractionStep[] Steps { get; set; } = Array.Empty<InteractionStep>();
    public TimeSpan ExpectedDuration { get; set; }
}

/// <summary>
/// Individual interaction step in a workflow
/// </summary>
public class InteractionStep
{
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string ExpectedResult { get; set; } = string.Empty;
}

/// <summary>
/// Test data model for category hierarchy
/// </summary>
public class CategoryHierarchyTestData
{
    public CategoryTestData[] Categories { get; set; } = Array.Empty<CategoryTestData>();
    public Dictionary<Guid, Guid[]> ParentChildMapping { get; set; } = new();
    public int MaxDepth { get; set; }
}
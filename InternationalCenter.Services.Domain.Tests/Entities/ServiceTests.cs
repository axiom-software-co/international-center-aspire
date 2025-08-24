using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Tests.Shared.TestData;

namespace InternationalCenter.Services.Domain.Tests.Entities;

/// <summary>
/// Unit tests for Service domain entity
/// Tests business rules, invariants, and domain logic without external dependencies
/// </summary>
public class ServiceTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateService()
    {
        // Arrange
        var id = ServiceId.Create();
        var title = "Test Service";
        var slug = Slug.Create("test-service");
        var description = "Test description";
        var detailedDescription = "Detailed test description";
        var metadata = ServiceMetadata.Create(
            technologies: new[] { "testing", "unit-test" },
            features: new[] { "test", "service" },
            deliveryModes: new[] { "Online" }
        );

        // Act
        var service = new Service(id, title, slug, description, detailedDescription, metadata);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(id, service.Id);
        Assert.Equal(title, service.Title);
        Assert.Equal(slug, service.Slug);
        Assert.Equal(description, service.Description);
        Assert.Equal(detailedDescription, service.DetailedDescription);
        Assert.Equal(metadata, service.Metadata);
        Assert.Equal(ServiceStatus.Draft, service.Status);
        Assert.False(service.Featured);
        Assert.True(service.Available);
        Assert.True(service.CreatedAt <= DateTime.UtcNow);
        Assert.True(service.UpdatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        // Arrange
        var id = ServiceId.Create();
        var slug = Slug.Create("test-service");
        var description = "Test description";
        var detailedDescription = "Detailed test description";
        var metadata = ServiceMetadata.Create();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Service(id, invalidTitle, slug, description, detailedDescription, metadata));
    }

    [Fact]
    public void Publish_WithValidService_ShouldChangeStatusToPublished()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        var originalUpdatedAt = service.UpdatedAt;

        // Act
        service.Publish();

        // Assert
        Assert.Equal(ServiceStatus.Published, service.Status);
        Assert.True(service.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ShouldNotChangeUpdatedAt()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        service.Publish();
        var publishedUpdatedAt = service.UpdatedAt;

        // Act
        service.Publish();

        // Assert
        Assert.Equal(ServiceStatus.Published, service.Status);
        Assert.Equal(publishedUpdatedAt, service.UpdatedAt);
    }

    [Fact]
    public void Publish_WithEmptyDescription_ShouldThrowInvalidOperationException()
    {
        // Arrange - Create service with valid title but empty description
        var id = ServiceId.Create();
        var title = "Valid Title";
        var slug = Slug.Create("test-service");
        var description = ""; // Empty description allowed in constructor
        var metadata = ServiceMetadata.Create();
        var service = new Service(id, title, slug, description, "Detailed description", metadata);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.Publish());
        Assert.Equal("Service must have title and description to be published", exception.Message);
    }

    [Fact]
    public void Publish_WithWhitespaceDescription_ShouldThrowInvalidOperationException()
    {
        // Arrange - Create service with valid title but whitespace description
        var id = ServiceId.Create();
        var title = "Valid Title";
        var slug = Slug.Create("test-service");
        var description = "   "; // Whitespace description
        var metadata = ServiceMetadata.Create();
        var service = new Service(id, title, slug, description, "Detailed description", metadata);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.Publish());
        Assert.Equal("Service must have title and description to be published", exception.Message);
    }

    [Fact]
    public void SetFeatured_WhenPublished_ShouldSetFeaturedFlag()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        service.Publish();

        // Act
        service.SetFeatured(true);

        // Assert
        Assert.True(service.Featured);
    }

    [Fact]
    public void SetFeatured_WithFalse_ShouldRemoveFeaturedFlag()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        service.Publish();
        service.SetFeatured(true);

        // Act
        service.SetFeatured(false);

        // Assert
        Assert.False(service.Featured);
    }

    [Fact]
    public void Archive_ShouldChangeStatusToArchivedAndSetUnavailable()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        service.Publish();
        var originalUpdatedAt = service.UpdatedAt;

        // Act
        service.Archive();

        // Assert
        Assert.Equal(ServiceStatus.Archived, service.Status);
        Assert.False(service.Available);
        Assert.True(service.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void UnPublish_ShouldChangeStatusToDraft()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        service.Publish();
        var originalUpdatedAt = service.UpdatedAt;

        // Act
        service.UnPublish();

        // Assert
        Assert.Equal(ServiceStatus.Draft, service.Status);
        Assert.True(service.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void UpdateTitle_WithValidTitle_ShouldUpdateTitle()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        var newTitle = "Updated Service Title";
        var originalUpdatedAt = service.UpdatedAt;

        // Act
        service.UpdateTitle(newTitle);

        // Assert
        Assert.Equal(newTitle, service.Title);
        Assert.True(service.UpdatedAt > originalUpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateTitle_WithInvalidTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.UpdateTitle(invalidTitle));
    }

    [Fact]
    public void SetCategory_WithValidCategoryId_ShouldUpdateCategory()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        var categoryId = ServiceCategoryId.Create(123);
        var originalUpdatedAt = service.UpdatedAt;

        // Act
        service.SetCategory(categoryId);

        // Assert
        Assert.Equal(categoryId, service.CategoryId);
        Assert.True(service.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void SetAvailability_ShouldUpdateAvailability()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        var originalUpdatedAt = service.UpdatedAt;

        // Act
        service.SetAvailability(false);

        // Assert
        Assert.False(service.Available);
        Assert.True(service.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void IsActive_WhenPublishedAndAvailable_ShouldReturnTrue()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        service.Publish();
        service.SetAvailability(true);

        // Act & Assert
        Assert.True(service.IsActive);
    }

    [Theory]
    [InlineData(false, true)] // Not published but available
    [InlineData(true, false)] // Published but not available
    [InlineData(false, false)] // Neither published nor available
    public void IsActive_WhenNotPublishedOrNotAvailable_ShouldReturnFalse(bool shouldPublish, bool shouldSetAvailable)
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        
        if (shouldPublish)
            service.Publish();
            
        service.SetAvailability(shouldSetAvailable);

        // Act & Assert
        Assert.False(service.IsActive);
    }
}
using InternationalCenter.Services.Domain.ValueObjects;

namespace InternationalCenter.Services.Domain.Tests.ValueObjects;

/// <summary>
/// Unit tests for ServiceMetadata value object
/// Tests validation rules, immutability, and update methods
/// </summary>
public class ServiceMetadataTests
{
    [Fact]
    public void Create_WithDefaultValues_ShouldCreateValidMetadata()
    {
        // Act
        var metadata = ServiceMetadata.Create();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(string.Empty, metadata.Icon);
        Assert.Equal(string.Empty, metadata.Image);
        Assert.Equal(string.Empty, metadata.MetaTitle);
        Assert.Equal(string.Empty, metadata.MetaDescription);
        Assert.Empty(metadata.Technologies);
        Assert.Empty(metadata.Features);
        Assert.Empty(metadata.DeliveryModes);
    }

    [Fact]
    public void Create_WithValidData_ShouldCreateMetadata()
    {
        // Arrange
        var icon = "https://example.com/icon.png";
        var image = "https://example.com/image.jpg";
        var metaTitle = "Test Service";
        var metaDescription = "A test service description";
        var technologies = new[] { "C#", "PostgreSQL" };
        var features = new[] { "API", "Caching" };
        var deliveryModes = new[] { "Online", "Hybrid" };

        // Act
        var metadata = ServiceMetadata.Create(
            icon: icon,
            image: image,
            metaTitle: metaTitle,
            metaDescription: metaDescription,
            technologies: technologies,
            features: features,
            deliveryModes: deliveryModes);

        // Assert
        Assert.Equal(icon, metadata.Icon);
        Assert.Equal(image, metadata.Image);
        Assert.Equal(metaTitle, metadata.MetaTitle);
        Assert.Equal(metaDescription, metadata.MetaDescription);
        Assert.Equal(technologies, metadata.Technologies);
        Assert.Equal(features, metadata.Features);
        Assert.Equal(deliveryModes, metadata.DeliveryModes);
    }

    [Fact]
    public void Create_WithNullCollections_ShouldCreateEmptyCollections()
    {
        // Act
        var metadata = ServiceMetadata.Create(
            technologies: null,
            features: null,
            deliveryModes: null);

        // Assert
        Assert.Empty(metadata.Technologies);
        Assert.Empty(metadata.Features);
        Assert.Empty(metadata.DeliveryModes);
    }

    [Fact]
    public void Create_WithWhitespaceInCollections_ShouldFilterOut()
    {
        // Arrange
        var technologies = new[] { "C#", "", "   ", null, "PostgreSQL" };
        var features = new[] { "API", "  ", "", "Caching" };

        // Act
        var metadata = ServiceMetadata.Create(
            technologies: technologies,
            features: features);

        // Assert
        Assert.Equal(new[] { "C#", "PostgreSQL" }, metadata.Technologies);
        Assert.Equal(new[] { "API", "Caching" }, metadata.Features);
    }

    [Theory]
    [InlineData(501)] // Just over limit
    [InlineData(1000)] // Well over limit
    public void Create_WithTooLongIcon_ShouldThrowArgumentException(int iconLength)
    {
        // Arrange
        var longIcon = new string('x', iconLength);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ServiceMetadata.Create(icon: longIcon));
        Assert.Contains("URL too long", exception.Message);
    }

    [Theory]
    [InlineData(501)] // Just over limit
    [InlineData(1000)] // Well over limit
    public void Create_WithTooLongImage_ShouldThrowArgumentException(int imageLength)
    {
        // Arrange
        var longImage = new string('x', imageLength);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ServiceMetadata.Create(image: longImage));
        Assert.Contains("URL too long", exception.Message);
    }

    [Theory]
    [InlineData(256)] // Just over limit
    [InlineData(500)] // Well over limit
    public void Create_WithTooLongMetaTitle_ShouldThrowArgumentException(int titleLength)
    {
        // Arrange
        var longTitle = new string('x', titleLength);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ServiceMetadata.Create(metaTitle: longTitle));
        Assert.Contains("Meta title too long", exception.Message);
    }

    [Theory]
    [InlineData(501)] // Just over limit
    [InlineData(1000)] // Well over limit
    public void Create_WithTooLongMetaDescription_ShouldThrowArgumentException(int descriptionLength)
    {
        // Arrange
        var longDescription = new string('x', descriptionLength);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ServiceMetadata.Create(metaDescription: longDescription));
        Assert.Contains("Meta description too long", exception.Message);
    }

    [Fact]
    public void UpdateIcon_WithValidIcon_ShouldReturnNewInstanceWithUpdatedIcon()
    {
        // Arrange
        var originalMetadata = ServiceMetadata.Create(
            icon: "original-icon.png",
            image: "image.jpg",
            metaTitle: "Title");
        var newIcon = "new-icon.png";

        // Act
        var updatedMetadata = originalMetadata.UpdateIcon(newIcon);

        // Assert
        Assert.NotSame(originalMetadata, updatedMetadata); // Different instances
        Assert.Equal(newIcon, updatedMetadata.Icon);
        Assert.Equal(originalMetadata.Image, updatedMetadata.Image);
        Assert.Equal(originalMetadata.MetaTitle, updatedMetadata.MetaTitle);
    }

    [Fact]
    public void UpdateIcon_WithTooLongIcon_ShouldThrowArgumentException()
    {
        // Arrange
        var metadata = ServiceMetadata.Create();
        var longIcon = new string('x', 501);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            metadata.UpdateIcon(longIcon));
        Assert.Contains("URL too long", exception.Message);
    }

    [Fact]
    public void UpdateImage_WithValidImage_ShouldReturnNewInstanceWithUpdatedImage()
    {
        // Arrange
        var originalMetadata = ServiceMetadata.Create(
            icon: "icon.png",
            image: "original-image.jpg",
            metaTitle: "Title");
        var newImage = "new-image.jpg";

        // Act
        var updatedMetadata = originalMetadata.UpdateImage(newImage);

        // Assert
        Assert.NotSame(originalMetadata, updatedMetadata); // Different instances
        Assert.Equal(newImage, updatedMetadata.Image);
        Assert.Equal(originalMetadata.Icon, updatedMetadata.Icon);
        Assert.Equal(originalMetadata.MetaTitle, updatedMetadata.MetaTitle);
    }

    [Fact]
    public void UpdateSeo_WithValidData_ShouldReturnNewInstanceWithUpdatedSeoData()
    {
        // Arrange
        var originalMetadata = ServiceMetadata.Create(
            metaTitle: "Original Title",
            metaDescription: "Original Description");
        var newTitle = "Updated Title";
        var newDescription = "Updated Description";

        // Act
        var updatedMetadata = originalMetadata.UpdateSeo(newTitle, newDescription);

        // Assert
        Assert.NotSame(originalMetadata, updatedMetadata); // Different instances
        Assert.Equal(newTitle, updatedMetadata.MetaTitle);
        Assert.Equal(newDescription, updatedMetadata.MetaDescription);
        Assert.Equal(originalMetadata.Icon, updatedMetadata.Icon);
        Assert.Equal(originalMetadata.Image, updatedMetadata.Image);
    }

    [Fact]
    public void UpdateSeo_WithTooLongTitle_ShouldThrowArgumentException()
    {
        // Arrange
        var metadata = ServiceMetadata.Create();
        var longTitle = new string('x', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            metadata.UpdateSeo(longTitle, "Valid description"));
        Assert.Contains("Meta title too long", exception.Message);
    }

    [Fact]
    public void UpdateSeo_WithTooLongDescription_ShouldThrowArgumentException()
    {
        // Arrange
        var metadata = ServiceMetadata.Create();
        var longDescription = new string('x', 501);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            metadata.UpdateSeo("Valid title", longDescription));
        Assert.Contains("Meta description too long", exception.Message);
    }

    [Fact]
    public void ServiceMetadata_ShouldBeImmutable()
    {
        // Arrange
        var technologies = new List<string> { "C#" };
        var metadata = ServiceMetadata.Create(technologies: technologies);

        // Act - Try to modify the original list
        technologies.Add("Java");

        // Assert - Original metadata should be unchanged
        Assert.Single(metadata.Technologies);
        Assert.Equal("C#", metadata.Technologies.First());
    }
}
using InternationalCenter.Services.Domain.ValueObjects;

namespace InternationalCenter.Services.Domain.Tests.ValueObjects;

/// <summary>
/// Unit tests for ServiceStatus enum and extensions
/// Tests string conversion and parsing logic
/// </summary>
public class ServiceStatusTests
{
    [Theory]
    [InlineData(ServiceStatus.Draft, "draft")]
    [InlineData(ServiceStatus.Published, "published")]
    [InlineData(ServiceStatus.Archived, "archived")]
    public void ToStringValue_WithValidStatus_ShouldReturnCorrectString(ServiceStatus status, string expected)
    {
        // Act
        var result = status.ToStringValue();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("draft", ServiceStatus.Draft)]
    [InlineData("published", ServiceStatus.Published)]
    [InlineData("archived", ServiceStatus.Archived)]
    [InlineData("active", ServiceStatus.Published)] // Legacy support
    public void FromString_WithValidString_ShouldReturnCorrectStatus(string input, ServiceStatus expected)
    {
        // Act
        var result = ServiceStatusExtensions.FromString(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("DRAFT", ServiceStatus.Draft)]
    [InlineData("Published", ServiceStatus.Published)]
    [InlineData("ARCHIVED", ServiceStatus.Archived)]
    [InlineData("Active", ServiceStatus.Published)] // Legacy support
    public void FromString_WithCaseInsensitiveString_ShouldReturnCorrectStatus(string input, ServiceStatus expected)
    {
        // Act
        var result = ServiceStatusExtensions.FromString(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("pending")]
    public void FromString_WithInvalidString_ShouldThrowArgumentException(string invalidStatus)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ServiceStatusExtensions.FromString(invalidStatus));
        Assert.Contains("Invalid service status", exception.Message);
        Assert.Contains(invalidStatus, exception.Message);
    }

    [Fact]
    public void FromString_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ServiceStatusExtensions.FromString(null));
        Assert.Contains("Invalid service status", exception.Message);
    }

    [Fact]
    public void ToStringValue_WithInvalidStatus_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var invalidStatus = (ServiceStatus)999;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            invalidStatus.ToStringValue());
    }

    [Fact]
    public void StatusConversion_ShouldBeRoundTripCompatible()
    {
        // Arrange
        var originalStatuses = new[] { ServiceStatus.Draft, ServiceStatus.Published, ServiceStatus.Archived };

        foreach (var originalStatus in originalStatuses)
        {
            // Act
            var stringValue = originalStatus.ToStringValue();
            var convertedStatus = ServiceStatusExtensions.FromString(stringValue);

            // Assert
            Assert.Equal(originalStatus, convertedStatus);
        }
    }

    [Theory]
    [InlineData("   draft   ", ServiceStatus.Draft)]
    [InlineData("  PUBLISHED  ", ServiceStatus.Published)]
    [InlineData("\tarchived\t", ServiceStatus.Archived)]
    public void FromString_WithWhitespace_ShouldIgnoreWhitespace(string input, ServiceStatus expected)
    {
        // Act
        var result = ServiceStatusExtensions.FromString(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LegacySupport_ActiveToPublished_ShouldWork()
    {
        // Act - Test that legacy "active" maps to Published
        var result = ServiceStatusExtensions.FromString("active");

        // Assert
        Assert.Equal(ServiceStatus.Published, result);
    }

    [Fact]
    public void AllEnumValues_ShouldHaveStringRepresentation()
    {
        // Arrange
        var allStatuses = Enum.GetValues<ServiceStatus>();

        foreach (var status in allStatuses)
        {
            // Act & Assert - Should not throw
            var stringValue = status.ToStringValue();
            Assert.NotNull(stringValue);
            Assert.False(string.IsNullOrWhiteSpace(stringValue));
        }
    }
}
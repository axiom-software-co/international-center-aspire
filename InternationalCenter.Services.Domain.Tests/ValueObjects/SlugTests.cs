using System.Linq;
using InternationalCenter.Services.Domain.ValueObjects;

namespace InternationalCenter.Services.Domain.Tests.ValueObjects;

/// <summary>
/// Unit tests for Slug value object
/// Tests validation rules, normalization logic, and string conversion
/// </summary>
public class SlugTests
{
    [Theory]
    [InlineData("valid-slug")]
    [InlineData("another-valid-slug")]
    [InlineData("slug123")]
    [InlineData("123-slug")]
    [InlineData("a")]
    [InlineData("123")]
    [InlineData("multi-word-slug-123")]
    public void Create_WithValidSlug_ShouldCreateSlug(string validSlug)
    {
        // Act
        var slug = Slug.Create(validSlug);

        // Assert
        Assert.NotNull(slug);
        Assert.Equal(validSlug, slug.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespace_ShouldThrowArgumentException(string invalidValue)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Slug.Create(invalidValue));
        Assert.Contains("Slug cannot be empty", exception.Message);
    }

    [Theory]
    [InlineData("Invalid Slug", "invalid-slug")]  // Uppercase and space normalized
    [InlineData("invalid_slug", "invalidslug")]   // Underscore removed
    [InlineData("invalid.slug", "invalidslug")]   // Dot removed
    [InlineData("invalid@slug", "invalidslug")]   // Special character removed
    [InlineData("-invalid", "invalid")]           // Leading hyphen trimmed
    [InlineData("invalid-", "invalid")]           // Trailing hyphen trimmed
    [InlineData("in--valid", "in-valid")]         // Double hyphen normalized
    public void Create_WithNormalizableInput_ShouldNormalizeAndSucceed(string input, string expectedSlug)
    {
        // Act
        var slug = Slug.Create(input);

        // Assert
        Assert.Equal(expectedSlug, slug.Value);
    }

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("The Quick Brown Fox", "the-quick-brown-fox")]
    [InlineData("Service   with   Spaces", "service-with-spaces")]
    [InlineData("UPPERCASE TITLE", "uppercase-title")]
    [InlineData("Mixed-Case Title", "mixed-case-title")]
    public void FromTitle_WithValidTitle_ShouldCreateNormalizedSlug(string title, string expectedSlug)
    {
        // Act
        var slug = Slug.FromTitle(title);

        // Assert
        Assert.Equal(expectedSlug, slug.Value);
    }

    [Theory]
    [InlineData("Title with Special@Characters!", "title-with-specialcharacters")]
    [InlineData("Title & More # Characters $", "title-more-characters")]
    [InlineData("C# Programming Tutorial", "c-programming-tutorial")]
    [InlineData("API Design: Best Practices", "api-design-best-practices")]
    public void FromTitle_WithSpecialCharacters_ShouldRemoveSpecialCharacters(string title, string expectedSlug)
    {
        // Act
        var slug = Slug.FromTitle(title);

        // Assert
        Assert.Equal(expectedSlug, slug.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FromTitle_WithEmptyTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Slug.FromTitle(invalidTitle));
        Assert.Contains("Title cannot be empty", exception.Message);
    }

    [Fact]
    public void FromTitle_WithLongTitle_ShouldTruncateToMaxLength()
    {
        // Arrange
        var longTitle = new string('a', 300) + " " + new string('b', 50); // Creates a very long title

        // Act
        var slug = Slug.FromTitle(longTitle);

        // Assert
        Assert.True(slug.Value.Length <= 255);
        Assert.False(slug.Value.EndsWith('-')); // Should trim trailing hyphens
    }

    [Fact]
    public void FromTitle_WithMultipleSpacesAndHyphens_ShouldNormalizeToSingleHyphens()
    {
        // Arrange
        var title = "Multiple   Spaces    And---Hyphens";

        // Act
        var slug = Slug.FromTitle(title);

        // Assert
        Assert.Equal("multiple-spaces-and-hyphens", slug.Value);
    }

    [Fact]
    public void FromTitle_WithLeadingAndTrailingSpaces_ShouldTrimAndNormalize()
    {
        // Arrange
        var title = "   Leading And Trailing Spaces   ";

        // Act
        var slug = Slug.FromTitle(title);

        // Assert
        Assert.Equal("leading-and-trailing-spaces", slug.Value);
    }

    [Fact]
    public void ImplicitStringConversion_ShouldReturnValue()
    {
        // Arrange
        var slug = Slug.Create("test-slug");

        // Act
        string stringValue = slug;

        // Assert
        Assert.Equal("test-slug", stringValue);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var slug = Slug.Create("test-slug");

        // Act
        var stringValue = slug.ToString();

        // Assert
        Assert.Equal("test-slug", stringValue);
    }

    [Fact]
    public void ValueEquality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var slug1 = Slug.Create("same-slug");
        var slug2 = Slug.Create("same-slug");

        // Act & Assert
        Assert.Equal(slug1, slug2);
        Assert.True(slug1 == slug2);
        Assert.False(slug1 != slug2);
        Assert.Equal(slug1.GetHashCode(), slug2.GetHashCode());
    }

    [Fact]
    public void ValueEquality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var slug1 = Slug.Create("different-slug");
        var slug2 = Slug.Create("another-slug");

        // Act & Assert
        Assert.NotEqual(slug1, slug2);
        Assert.False(slug1 == slug2);
        Assert.True(slug1 != slug2);
    }

    [Theory]
    [InlineData("Title with Numbers 123", "title-with-numbers-123")]
    [InlineData("2024 Product Launch", "2024-product-launch")]
    [InlineData("Version 2.0.1 Release", "version-201-release")]
    public void FromTitle_WithNumbers_ShouldPreserveNumbers(string title, string expectedSlug)
    {
        // Act
        var slug = Slug.FromTitle(title);

        // Assert
        Assert.Equal(expectedSlug, slug.Value);
    }

    [Theory]
    [InlineData("@#$%^&*()!")]  // Only special characters - normalizes to empty
    [InlineData("---")]         // Only hyphens - normalizes to empty  
    public void Create_WithInputThatNormalizesToEmpty_ShouldThrowInvalidFormatException(string input)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Slug.Create(input));
        Assert.Contains("Invalid slug format", exception.Message);
    }

    [Theory]
    [InlineData("   ")]         // Only spaces - caught by initial whitespace check
    public void Create_WithWhitespaceInput_ShouldThrowEmptyException(string input)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Slug.Create(input));
        Assert.Contains("Slug cannot be empty", exception.Message);
    }


    [Theory]
    [InlineData("Title-with-existing-hyphens", "title-with-existing-hyphens")]
    [InlineData("Pre--formatted--slug", "pre-formatted-slug")]
    [InlineData("--title--with--edge--hyphens--", "title-with-edge-hyphens")]
    public void FromTitle_WithExistingHyphens_ShouldNormalizeProperly(string title, string expectedSlug)
    {
        // Act
        var slug = Slug.FromTitle(title);

        // Assert
        Assert.Equal(expectedSlug, slug.Value);
    }

    [Fact]
    public void Create_WithMaxLengthSlug_ShouldSucceed()
    {
        // Arrange
        var maxLengthSlug = new string('a', 255);

        // Act
        var slug = Slug.Create(maxLengthSlug);

        // Assert
        Assert.Equal(maxLengthSlug, slug.Value);
        Assert.Equal(255, slug.Value.Length);
    }

    [Fact]
    public void Create_WithSlugExceedingMaxLength_ShouldTruncateToMaxLength()
    {
        // Arrange - Create a slug that's too long
        var tooLongSlug = new string('a', 256);

        // Act
        var slug = Slug.Create(tooLongSlug);

        // Assert
        Assert.Equal(255, slug.Value.Length);
        Assert.True(slug.Value.All(c => c == 'a')); // Should be all 'a's, truncated
    }

    [Theory]
    [InlineData("Café Restaurant", "caf-restaurant")]
    [InlineData("Naïve Approach", "nave-approach")]
    [InlineData("Résumé Builder", "rsum-builder")]
    public void FromTitle_WithAccentedCharacters_ShouldRemoveAccents(string title, string expectedSlug)
    {
        // Act
        var slug = Slug.FromTitle(title);

        // Assert
        Assert.Equal(expectedSlug, slug.Value);
    }

    [Fact]
    public void SlugRegexPattern_ShouldOnlyAllowValidCharacters()
    {
        // This test validates the regex pattern behavior
        
        // Valid patterns - should not throw
        var validSlug1 = Slug.Create("valid-slug");
        var validSlug2 = Slug.Create("123-abc");
        var validSlug3 = Slug.Create("abc-123-def");
        
        Assert.NotNull(validSlug1);
        Assert.NotNull(validSlug2);
        Assert.NotNull(validSlug3);
        
        // These get normalized, so test the actual normalized results instead
        var normalizedUpper = Slug.Create("ABC");
        var normalizedUnder = Slug.Create("slug_with_underscores");
        var normalizedDots = Slug.Create("slug.with.dots");
        var normalizedSpaces = Slug.Create("slug with spaces");
        
        Assert.Equal("abc", normalizedUpper.Value);
        Assert.Equal("slugwithunderscores", normalizedUnder.Value);
        Assert.Equal("slugwithdots", normalizedDots.Value);
        Assert.Equal("slug-with-spaces", normalizedSpaces.Value);
    }
}
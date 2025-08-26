using System.Text.RegularExpressions;

namespace Services.Shared.ValueObjects;

public sealed record Slug
{
    private static readonly Regex SlugRegex = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    
    public string Value { get; }

    private Slug(string value)
    {
        Value = value;
    }

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slug cannot be empty", nameof(value));

        var normalizedValue = Normalize(value);
        
        if (!IsValid(normalizedValue))
            throw new ArgumentException("Invalid slug format. Must contain only lowercase letters, numbers, and hyphens", nameof(value));

        return new Slug(normalizedValue);
    }

    public static Slug FromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        var slug = Normalize(title);
        return new Slug(slug);
    }

    private static string Normalize(string input)
    {
        // Convert to lowercase
        var normalized = input.ToLowerInvariant();
        
        // Replace spaces and special characters with hyphens
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", "");
        normalized = Regex.Replace(normalized, @"[\s-]+", "-");
        
        // Remove leading/trailing hyphens
        normalized = normalized.Trim('-');
        
        // Truncate if too long
        if (normalized.Length > 255)
            normalized = normalized.Substring(0, 255).TrimEnd('-');

        return normalized;
    }

    private static bool IsValid(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && 
               value.Length <= 255 && 
               SlugRegex.IsMatch(value);
    }

    public static implicit operator string(Slug slug) => slug.Value;

    public override string ToString() => Value;
}
namespace InternationalCenter.Services.Domain.ValueObjects;

public sealed record ServiceMetadata
{
    public string Icon { get; }
    public string Image { get; }
    public string MetaTitle { get; }
    public string MetaDescription { get; }
    public IReadOnlyList<string> Technologies { get; }
    public IReadOnlyList<string> Features { get; }
    public IReadOnlyList<string> DeliveryModes { get; }

    private ServiceMetadata(
        string icon,
        string image,
        string metaTitle,
        string metaDescription,
        IReadOnlyList<string> technologies,
        IReadOnlyList<string> features,
        IReadOnlyList<string> deliveryModes)
    {
        Icon = icon;
        Image = image;
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
        Technologies = technologies;
        Features = features;
        DeliveryModes = deliveryModes;
    }

    public static ServiceMetadata Create(
        string? icon = null,
        string? image = null,
        string? metaTitle = null,
        string? metaDescription = null,
        IEnumerable<string>? technologies = null,
        IEnumerable<string>? features = null,
        IEnumerable<string>? deliveryModes = null)
    {
        return new ServiceMetadata(
            ValidateUrl(icon ?? string.Empty, nameof(icon)),
            ValidateUrl(image ?? string.Empty, nameof(image)),
            ValidateMetaTitle(metaTitle ?? string.Empty),
            ValidateMetaDescription(metaDescription ?? string.Empty),
            (technologies ?? Array.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList().AsReadOnly(),
            (features ?? Array.Empty<string>()).Where(f => !string.IsNullOrWhiteSpace(f)).ToList().AsReadOnly(),
            (deliveryModes ?? Array.Empty<string>()).Where(d => !string.IsNullOrWhiteSpace(d)).ToList().AsReadOnly());
    }

    private static string ValidateUrl(string url, string paramName)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        if (url.Length > 500)
            throw new ArgumentException($"URL too long (max 500 characters)", paramName);

        return url;
    }

    private static string ValidateMetaTitle(string metaTitle)
    {
        if (string.IsNullOrEmpty(metaTitle))
            return string.Empty;

        if (metaTitle.Length > 255)
            throw new ArgumentException("Meta title too long (max 255 characters)");

        return metaTitle;
    }

    private static string ValidateMetaDescription(string metaDescription)
    {
        if (string.IsNullOrEmpty(metaDescription))
            return string.Empty;

        if (metaDescription.Length > 500)
            throw new ArgumentException("Meta description too long (max 500 characters)");

        return metaDescription;
    }

    public ServiceMetadata UpdateIcon(string icon)
    {
        return new ServiceMetadata(
            ValidateUrl(icon, nameof(icon)), 
            Image, 
            MetaTitle, 
            MetaDescription, 
            Technologies, 
            Features, 
            DeliveryModes);
    }

    public ServiceMetadata UpdateImage(string image)
    {
        return new ServiceMetadata(
            Icon, 
            ValidateUrl(image, nameof(image)), 
            MetaTitle, 
            MetaDescription, 
            Technologies, 
            Features, 
            DeliveryModes);
    }

    public ServiceMetadata UpdateSeo(string metaTitle, string metaDescription)
    {
        return new ServiceMetadata(
            Icon, 
            Image, 
            ValidateMetaTitle(metaTitle), 
            ValidateMetaDescription(metaDescription), 
            Technologies, 
            Features, 
            DeliveryModes);
    }
}
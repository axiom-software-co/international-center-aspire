namespace Services.Shared.ValueObjects;

public sealed record ServiceId
{
    public string Value { get; }

    private ServiceId(string value)
    {
        Value = value;
    }

    public static ServiceId Create(string? value = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new ServiceId(Guid.NewGuid().ToString());

        if (!IsValid(value))
            throw new ArgumentException("Invalid service ID format", nameof(value));

        return new ServiceId(value);
    }

    public static ServiceId FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Service ID cannot be empty", nameof(value));

        if (!IsValid(value))
            throw new ArgumentException("Invalid service ID format", nameof(value));

        return new ServiceId(value);
    }

    private static bool IsValid(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= 255;
    }

    public static implicit operator string(ServiceId serviceId) => serviceId.Value;

    public override string ToString() => Value;
}
namespace InternationalCenter.Services.Domain.ValueObjects;

public sealed record ServiceCategoryId
{
    public int Value { get; }

    private ServiceCategoryId(int value)
    {
        Value = value;
    }

    public static ServiceCategoryId Create(int value)
    {
        if (value <= 0)
            throw new ArgumentException("Service category ID must be greater than zero", nameof(value));

        return new ServiceCategoryId(value);
    }

    public static implicit operator int(ServiceCategoryId categoryId) => categoryId.Value;

    public override string ToString() => Value.ToString();
}
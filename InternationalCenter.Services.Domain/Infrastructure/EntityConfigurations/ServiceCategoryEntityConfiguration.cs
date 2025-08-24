using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternationalCenter.Services.Domain.Infrastructure.EntityConfigurations;

public sealed class ServiceCategoryEntityConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        // Table mapping
        builder.ToTable("service_categories");

        // Primary key
        builder.HasKey(sc => sc.Id);

        // Value object conversions
        builder.Property(sc => sc.Id)
            .HasConversion(
                id => id.Value,
                value => ServiceCategoryId.Create(value))
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(sc => sc.Slug)
            .HasConversion(
                slug => slug.Value,
                value => Slug.Create(value))
            .IsRequired()
            .HasMaxLength(255);

        // Simple properties
        builder.Property(sc => sc.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(sc => sc.Description)
            .HasDefaultValue(string.Empty);

        builder.Property(sc => sc.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(sc => sc.Active)
            .IsRequired()
            .HasDefaultValue(true);

        // Timestamps
        builder.Property(sc => sc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(sc => sc.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasMany(sc => sc.Services)
            .WithOne(s => s.Category)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique constraints
        builder.HasIndex(sc => sc.Slug)
            .IsUnique();

        // Performance indexes
        builder.HasIndex(sc => new { sc.Active, sc.DisplayOrder })
            .HasDatabaseName("IX_ServiceCategories_Performance");
    }
}
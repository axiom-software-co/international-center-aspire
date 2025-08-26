using Services.Shared.Entities;
using Services.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Services.Shared.Infrastructure.EntityConfigurations;

public sealed class ServiceEntityConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        // Table mapping
        builder.ToTable("services");

        // Primary key
        builder.HasKey(s => s.Id);

        // Value object conversions
        builder.Property(s => s.Id)
            .HasConversion(
                id => id.Value,
                value => ServiceId.FromString(value))
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.Slug)
            .HasConversion(
                slug => slug.Value,
                value => Slug.Create(value))
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.CategoryId)
            .HasConversion(
                id => id == null ? (int?)null : id.Value,
                value => value.HasValue ? ServiceCategoryId.Create(value.Value) : null)
            .IsRequired(false);

        // Complex property mappings
        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Description)
            .HasDefaultValue(string.Empty);

        builder.Property(s => s.DetailedDescription)
            .HasDefaultValue(string.Empty);

        // ServiceMetadata as owned entity
        builder.OwnsOne(s => s.Metadata, metadata =>
        {
            metadata.Property(m => m.Icon)
                .HasColumnName("icon")
                .HasMaxLength(255)
                .HasDefaultValue(string.Empty);

            metadata.Property(m => m.Image)
                .HasColumnName("image")
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            metadata.Property(m => m.MetaTitle)
                .HasColumnName("meta_title")
                .HasMaxLength(255)
                .HasDefaultValue(string.Empty);

            metadata.Property(m => m.MetaDescription)
                .HasColumnName("meta_description")
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            // JSON columns for arrays (PostgreSQL specific)
            metadata.Property(m => m.Technologies)
                .HasColumnName("technologies")
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly())
                .HasMaxLength(1000);

            metadata.Property(m => m.Features)
                .HasColumnName("features")
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly())
                .HasMaxLength(1000);

            metadata.Property(m => m.DeliveryModes)
                .HasColumnName("delivery_modes")
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly())
                .HasMaxLength(500);
        });

        // Simple properties
        builder.Property(s => s.SortOrder)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(s => s.Available)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.Featured)
            .IsRequired()
            .HasDefaultValue(false);

        // Timestamps
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.Category)
            .WithMany(c => c.Services)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique constraints
        builder.HasIndex(s => s.Slug)
            .IsUnique();
    }
}
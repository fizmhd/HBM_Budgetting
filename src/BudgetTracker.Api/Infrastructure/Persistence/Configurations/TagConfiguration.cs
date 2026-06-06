using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Tag entity.
/// </summary>
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(50);

        // Owner / visibility scope (OwnedEntity).
        builder.Property(t => t.OwnerUserId)
            .IsRequired();

        builder.Property(t => t.Visibility)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.HouseholdId);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        // Tag names are unique per owner (case handled by normalising to lower-case on write).
        builder.HasIndex(t => new { t.OwnerUserId, t.Name })
            .IsUnique()
            .HasDatabaseName("IX_Tags_OwnerUserId_Name");

        builder.HasIndex(t => t.HouseholdId)
            .HasDatabaseName("IX_Tags_HouseholdId");
    }
}

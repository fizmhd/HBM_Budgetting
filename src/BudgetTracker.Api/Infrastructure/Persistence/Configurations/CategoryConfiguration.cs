using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Category entity (self-referencing tree).
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.Icon)
            .HasMaxLength(50);

        builder.Property(c => c.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        // Self-referencing FK. Restrict on delete: the service blocks deleting a parent that still
        // has children (CATEGORY_IN_USE), so we never want a cascade to silently remove a subtree.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Owner / visibility scope (OwnedEntity).
        builder.Property(c => c.OwnerUserId)
            .IsRequired();

        builder.Property(c => c.Visibility)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.HouseholdId);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Indexes that back the visibility filter and tree lookups within a scope.
        builder.HasIndex(c => new { c.OwnerUserId, c.ParentCategoryId })
            .HasDatabaseName("IX_Categories_OwnerUserId_ParentCategoryId");

        builder.HasIndex(c => new { c.HouseholdId, c.ParentCategoryId })
            .HasDatabaseName("IX_Categories_HouseholdId_ParentCategoryId");
    }
}

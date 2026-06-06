using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Budget entity (TASK 6.1).
/// </summary>
public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");

        builder.HasKey(b => b.Id);

        // CategoryId carries no hard FK on purpose: like transaction splits, category deletion is
        // governed by the service-layer reference check (CATEGORY_IN_USE), not a cascade.
        builder.Property(b => b.CategoryId)
            .IsRequired();

        builder.Property(b => b.PeriodType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(b => b.PeriodStart)
            .IsRequired();

        builder.Property(b => b.PeriodEnd)
            .IsRequired();

        builder.Property(b => b.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(b => b.AlertThresholdPercent)
            .IsRequired()
            .HasDefaultValue(80);

        builder.Property(b => b.LastAlertedThreshold)
            .IsRequired()
            .HasDefaultValue(0);

        // Owner / visibility scope (OwnedEntity).
        builder.Property(b => b.OwnerUserId)
            .IsRequired();

        builder.Property(b => b.Visibility)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(b => b.HouseholdId);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        // Indexes for the visibility filter and category lookups (spent calc + reference check).
        builder.HasIndex(b => b.OwnerUserId)
            .HasDatabaseName("IX_Budgets_OwnerUserId");

        builder.HasIndex(b => b.HouseholdId)
            .HasDatabaseName("IX_Budgets_HouseholdId");

        builder.HasIndex(b => b.CategoryId)
            .HasDatabaseName("IX_Budgets_CategoryId");
    }
}

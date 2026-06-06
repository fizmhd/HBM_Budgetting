using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the RecurringRule entity (TASK 5.1).
/// </summary>
public class RecurringRuleConfiguration : IEntityTypeConfiguration<RecurringRule>
{
    public void Configure(EntityTypeBuilder<RecurringRule> builder)
    {
        builder.ToTable("RecurringRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Type)
            .IsRequired()
            .HasConversion<int>();

        // AccountId/CategoryId carry no hard FK on purpose (consistent with transactions/budgets:
        // referential rules live in the service layer, not cascades).
        builder.Property(r => r.AccountId);
        builder.Property(r => r.CategoryId);

        builder.Property(r => r.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(r => r.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("SEK");

        builder.Property(r => r.Frequency)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Interval)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(r => r.DayOfMonth);

        builder.Property(r => r.StartDate).IsRequired();
        builder.Property(r => r.EndDate);
        builder.Property(r => r.NextDueDate).IsRequired();

        builder.Property(r => r.GenerationMode)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.IsSubscription)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.PausedAt);
        builder.Property(r => r.ResumedAt);

        // Occurrences are owned by the rule; deleting the rule removes them.
        builder.HasMany(r => r.Occurrences)
            .WithOne()
            .HasForeignKey(o => o.RecurringRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Owner / visibility scope (OwnedEntity).
        builder.Property(r => r.OwnerUserId).IsRequired();
        builder.Property(r => r.Visibility).IsRequired().HasConversion<int>();
        builder.Property(r => r.HouseholdId);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasIndex(r => r.OwnerUserId).HasDatabaseName("IX_RecurringRules_OwnerUserId");
        builder.HasIndex(r => r.HouseholdId).HasDatabaseName("IX_RecurringRules_HouseholdId");
        builder.HasIndex(r => r.CategoryId).HasDatabaseName("IX_RecurringRules_CategoryId");
        // The generation job scans active rules by due date.
        builder.HasIndex(r => new { r.Status, r.NextDueDate })
            .HasDatabaseName("IX_RecurringRules_Status_NextDueDate");
    }
}

using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the RecurringOccurrence entity (TASK 5.1).
/// </summary>
public class RecurringOccurrenceConfiguration : IEntityTypeConfiguration<RecurringOccurrence>
{
    public void Configure(EntityTypeBuilder<RecurringOccurrence> builder)
    {
        builder.ToTable("RecurringOccurrences");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.RecurringRuleId).IsRequired();
        builder.Property(o => o.DueDate).IsRequired();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.SkipReason).HasMaxLength(500);
        builder.Property(o => o.GeneratedTransactionId);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        // One occurrence per (rule, due date) — the backbone of idempotent generation (TASK 5.2).
        builder.HasIndex(o => new { o.RecurringRuleId, o.DueDate })
            .IsUnique()
            .HasDatabaseName("IX_RecurringOccurrences_Rule_DueDate");

        builder.HasIndex(o => o.Status).HasDatabaseName("IX_RecurringOccurrences_Status");
    }
}

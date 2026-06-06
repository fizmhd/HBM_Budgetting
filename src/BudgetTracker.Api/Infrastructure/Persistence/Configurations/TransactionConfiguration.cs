using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Transaction entity.
/// </summary>
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.AccountId)
            .IsRequired();

        builder.Property(t => t.Date)
            .IsRequired();

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(t => t.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("SEK");

        builder.Property(t => t.Description)
            .HasMaxLength(200);

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        builder.Property(t => t.CounterAccountId);

        // Splits are owned by the transaction; deleting the transaction removes its splits.
        builder.HasMany(t => t.Splits)
            .WithOne()
            .HasForeignKey(s => s.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.TransactionTags)
            .WithOne()
            .HasForeignKey(tt => tt.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // Indexes for the visibility filter and the common list filters (account, date).
        builder.HasIndex(t => t.OwnerUserId)
            .HasDatabaseName("IX_Transactions_OwnerUserId");

        builder.HasIndex(t => t.HouseholdId)
            .HasDatabaseName("IX_Transactions_HouseholdId");

        builder.HasIndex(t => t.AccountId)
            .HasDatabaseName("IX_Transactions_AccountId");

        builder.HasIndex(t => t.CounterAccountId)
            .HasDatabaseName("IX_Transactions_CounterAccountId");

        builder.HasIndex(t => t.Date)
            .HasDatabaseName("IX_Transactions_Date");
    }
}

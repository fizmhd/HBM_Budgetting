using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Account entity.
/// </summary>
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("SEK");

        builder.Property(a => a.OpeningBalance)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(a => a.CreditLimit)
            .HasPrecision(18, 2);

        builder.Property(a => a.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        // Owner / visibility scope (OwnedEntity).
        builder.Property(a => a.OwnerUserId)
            .IsRequired();

        builder.Property(a => a.Visibility)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.HouseholdId);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Indexes that back the visibility filter (owner lookups and household-shared lookups).
        builder.HasIndex(a => a.OwnerUserId)
            .HasDatabaseName("IX_Accounts_OwnerUserId");

        builder.HasIndex(a => a.HouseholdId)
            .HasDatabaseName("IX_Accounts_HouseholdId");
    }
}

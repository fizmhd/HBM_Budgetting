using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Payslip entity (TASK 8.1).
/// </summary>
public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.ToTable("Payslips");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Country)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.EmployerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.EmployeeName)
            .HasMaxLength(200);

        // Stored as base64 of the protected bytes; generous length headroom for the ciphertext.
        builder.Property(p => p.PersonnummerEncrypted)
            .HasMaxLength(1024);

        builder.Property(p => p.PersonnummerMasked)
            .HasMaxLength(64);

        builder.Property(p => p.PayPeriodStart).IsRequired();
        builder.Property(p => p.PayPeriodEnd).IsRequired();
        builder.Property(p => p.PayDate).IsRequired();

        builder.Property(p => p.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("SEK");

        builder.Property(p => p.DeclaredNet)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.PostedTransactionId);
        builder.Property(p => p.PostedAccountId);
        builder.Property(p => p.PostedAt);

        // Line items and leave balances are owned by the payslip; deleting it removes them.
        builder.HasMany(p => p.LineItems)
            .WithOne()
            .HasForeignKey(l => l.PayslipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.LeaveBalances)
            .WithOne()
            .HasForeignKey(b => b.PayslipId)
            .OnDelete(DeleteBehavior.Cascade);

        // Owner / visibility scope (OwnedEntity).
        builder.Property(p => p.OwnerUserId).IsRequired();
        builder.Property(p => p.Visibility).IsRequired().HasConversion<int>();
        builder.Property(p => p.HouseholdId);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.OwnerUserId).HasDatabaseName("IX_Payslips_OwnerUserId");
        builder.HasIndex(p => p.HouseholdId).HasDatabaseName("IX_Payslips_HouseholdId");
        // The YTD aggregation scans an owner's payslips within a pay-date year.
        builder.HasIndex(p => new { p.OwnerUserId, p.PayDate })
            .HasDatabaseName("IX_Payslips_OwnerUserId_PayDate");
    }
}

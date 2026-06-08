using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the PayslipLeaveBalance entity (TASK 8.1).
/// </summary>
public class PayslipLeaveBalanceConfiguration : IEntityTypeConfiguration<PayslipLeaveBalance>
{
    public void Configure(EntityTypeBuilder<PayslipLeaveBalance> builder)
    {
        builder.ToTable("PayslipLeaveBalances");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.PayslipId).IsRequired();

        builder.Property(b => b.LeaveType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Balance)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(b => b.Unit)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("days");

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        builder.HasIndex(b => b.PayslipId).HasDatabaseName("IX_PayslipLeaveBalances_PayslipId");
    }
}

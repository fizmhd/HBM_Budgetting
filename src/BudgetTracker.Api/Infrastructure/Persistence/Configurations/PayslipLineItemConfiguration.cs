using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the PayslipLineItem entity (TASK 8.1).
/// </summary>
public class PayslipLineItemConfiguration : IEntityTypeConfiguration<PayslipLineItem>
{
    public void Configure(EntityTypeBuilder<PayslipLineItem> builder)
    {
        builder.ToTable("PayslipLineItems");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.PayslipId).IsRequired();

        builder.Property(l => l.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(l => l.Label)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Quantity).HasPrecision(18, 2);
        builder.Property(l => l.UnitAmount).HasPrecision(18, 2);

        builder.Property(l => l.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(l => l.SortOrder).IsRequired().HasDefaultValue(0);

        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();

        builder.HasIndex(l => l.PayslipId).HasDatabaseName("IX_PayslipLineItems_PayslipId");
    }
}

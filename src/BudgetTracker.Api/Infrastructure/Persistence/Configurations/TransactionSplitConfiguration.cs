using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the TransactionSplit entity.
/// </summary>
public class TransactionSplitConfiguration : IEntityTypeConfiguration<TransactionSplit>
{
    public void Configure(EntityTypeBuilder<TransactionSplit> builder)
    {
        builder.ToTable("TransactionSplits");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TransactionId)
            .IsRequired();

        builder.Property(s => s.CategoryId)
            .IsRequired();

        builder.Property(s => s.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(s => s.Note)
            .HasMaxLength(200);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Backs the category-deletion reference check and category filtering.
        builder.HasIndex(s => s.CategoryId)
            .HasDatabaseName("IX_TransactionSplits_CategoryId");

        builder.HasIndex(s => s.TransactionId)
            .HasDatabaseName("IX_TransactionSplits_TransactionId");
    }
}

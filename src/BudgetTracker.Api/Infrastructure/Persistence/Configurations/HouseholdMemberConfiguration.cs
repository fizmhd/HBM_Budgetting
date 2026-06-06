using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the HouseholdMember entity.
/// </summary>
public class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.ToTable("HouseholdMembers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.HouseholdId)
            .IsRequired();

        // UserId is nullable on purpose (future child profiles); the MVP always sets it.
        builder.Property(m => m.UserId);

        builder.Property(m => m.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.JoinedAt)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        builder.HasIndex(m => m.HouseholdId)
            .HasDatabaseName("IX_HouseholdMembers_HouseholdId");

        // A given user can only appear once in a household.
        builder.HasIndex(m => new { m.HouseholdId, m.UserId })
            .IsUnique()
            .HasDatabaseName("IX_HouseholdMembers_HouseholdId_UserId");
    }
}

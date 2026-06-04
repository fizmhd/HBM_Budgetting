using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetTracker.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the UserExternalLogin entity
/// </summary>
public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        // Table name
        builder.ToTable("UserExternalLogins");

        // Primary key
        builder.HasKey(el => el.Id);

        // Properties
        builder.Property(el => el.UserId)
            .IsRequired();

        builder.Property(el => el.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(el => el.ProviderKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(el => el.ProviderEmail)
            .HasMaxLength(256);

        builder.Property(el => el.LastLoginAt)
            .IsRequired();

        builder.Property(el => el.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(el => el.CreatedAt)
            .IsRequired();

        builder.Property(el => el.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(el => new { el.Provider, el.ProviderKey })
            .IsUnique()
            .HasDatabaseName("IX_UserExternalLogins_Provider_ProviderKey");

        builder.HasIndex(el => el.UserId)
            .HasDatabaseName("IX_UserExternalLogins_UserId");

        builder.HasIndex(el => el.IsActive)
            .HasDatabaseName("IX_UserExternalLogins_IsActive");

        // Relationships
        builder.HasOne(el => el.User)
            .WithMany(u => u.ExternalLogins)
            .HasForeignKey(el => el.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

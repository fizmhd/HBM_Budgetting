namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Base class for every domain record that belongs to a user and carries a privacy scope.
/// Provides the uniform owner / visibility / household fields the authorization rule relies on:
/// a record is readable/writable by its owner, or by any household member when it is
/// <see cref="Visibility.HouseholdShared"/>.
/// </summary>
public abstract class OwnedEntity : BaseEntity
{
    /// <summary>
    /// Internal <see cref="User.Id"/> of the user that owns this record.
    /// </summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>
    /// Privacy scope of this record. Defaults to <see cref="Visibility.Individual"/> (private).
    /// </summary>
    public Visibility Visibility { get; set; } = Visibility.Individual;

    /// <summary>
    /// Household this record is associated with, when shared. Null for purely individual data.
    /// </summary>
    public Guid? HouseholdId { get; set; }
}

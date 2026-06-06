namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// An invitation for someone to join a household, identified by a unique shareable token.
/// </summary>
public class HouseholdInvite : BaseEntity
{
    /// <summary>
    /// Household the invitee is being invited to.
    /// </summary>
    public Guid HouseholdId { get; set; }

    /// <summary>
    /// Email address the invite was issued to.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Unique, unguessable token used to accept the invite (also the shareable link key).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Current lifecycle status of the invite.
    /// </summary>
    public InviteStatus Status { get; set; } = InviteStatus.Pending;

    /// <summary>
    /// When the invite expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    // Navigation properties
    /// <summary>
    /// The household this invite belongs to.
    /// </summary>
    public Household? Household { get; set; }
}

namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Lifecycle status of a household invite.
/// </summary>
public enum InviteStatus
{
    /// <summary>
    /// Invite issued and awaiting acceptance.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Invite was accepted; the invitee is now a member.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Invite was cancelled by the owner before acceptance.
    /// </summary>
    Revoked = 2,

    /// <summary>
    /// Invite passed its expiry without being accepted.
    /// </summary>
    Expired = 3
}

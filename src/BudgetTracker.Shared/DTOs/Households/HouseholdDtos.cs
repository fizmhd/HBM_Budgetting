namespace BudgetTracker.Shared.DTOs.Households;

/// <summary>
/// Request to create a new household.
/// </summary>
public class CreateHouseholdRequest
{
    /// <summary>
    /// Display name of the household.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Request to invite someone to a household by email.
/// </summary>
public class InviteMemberRequest
{
    /// <summary>
    /// Email address to invite.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// A member of a household.
/// </summary>
public class HouseholdMemberDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Role within the household ("Owner" or "Member").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// A household and its members.
/// </summary>
public class HouseholdDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public List<HouseholdMemberDto> Members { get; set; } = new();
}

/// <summary>
/// A pending invite, including the shareable token/link the invitee uses to join.
/// </summary>
public class HouseholdInviteDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The token to accept the invite (also forms the shareable link).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Status of the invite ("Pending", "Accepted", "Revoked", "Expired").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}

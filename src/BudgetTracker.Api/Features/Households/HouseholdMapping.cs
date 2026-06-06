using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.DTOs.Households;

namespace BudgetTracker.Api.Features.Households;

/// <summary>
/// Maps household entities to their DTOs.
/// </summary>
public static class HouseholdMapping
{
    public static HouseholdMemberDto ToDto(this HouseholdMember member) => new()
    {
        Id = member.Id,
        UserId = member.UserId,
        DisplayName = member.DisplayName,
        Role = member.Role.ToString(),
        JoinedAt = member.JoinedAt
    };

    public static HouseholdDto ToDto(this Household household, IEnumerable<HouseholdMember> members) => new()
    {
        Id = household.Id,
        Name = household.Name,
        CreatedByUserId = household.CreatedByUserId,
        Members = members
            .OrderBy(m => m.JoinedAt)
            .Select(m => m.ToDto())
            .ToList()
    };

    public static HouseholdInviteDto ToDto(this HouseholdInvite invite) => new()
    {
        Id = invite.Id,
        Email = invite.Email,
        Token = invite.Token,
        Status = invite.Status.ToString(),
        ExpiresAt = invite.ExpiresAt
    };

    /// <summary>
    /// Best-effort display name for a user (first/last name, falling back to the email prefix).
    /// </summary>
    public static string DisplayNameFor(User user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var atIndex = user.Email.IndexOf('@');
        return atIndex > 0 ? user.Email[..atIndex] : user.Email;
    }
}

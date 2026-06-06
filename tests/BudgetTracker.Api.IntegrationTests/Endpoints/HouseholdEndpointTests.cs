using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Households;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the Household API (TASK 1.3): create → invite → accept → list → remove,
/// plus authorization rules.
/// </summary>
public class HouseholdEndpointTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;

    public HouseholdEndpointTests(AuthenticatedWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Create_returns_household_with_caller_as_owner()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();

        var response = await owner.PostAsJsonAsync("/api/v1/households",
            new CreateHouseholdRequest { Name = "Andersson" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var household = await response.Content.ReadFromJsonAsync<HouseholdDto>();
        household!.Name.Should().Be("Andersson");
        household.Members.Should().ContainSingle();
        household.Members[0].Role.Should().Be("Owner");
    }

    [Fact]
    public async Task Creating_a_second_household_is_rejected()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        await owner.PostAsJsonAsync("/api/v1/households", new CreateHouseholdRequest { Name = "First" });

        var second = await owner.PostAsJsonAsync("/api/v1/households", new CreateHouseholdRequest { Name = "Second" });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Owner_can_invite_and_spouse_can_accept_and_both_appear_as_members()
    {
        // Owner creates a household.
        var owner = await _factory.CreateAuthenticatedClientAsync("owner@example.com");
        var created = await (await owner.PostAsJsonAsync("/api/v1/households",
            new CreateHouseholdRequest { Name = "Family" })).Content.ReadFromJsonAsync<HouseholdDto>();

        // Owner invites the spouse.
        var inviteResponse = await owner.PostAsJsonAsync($"/api/v1/households/{created!.Id}/invites",
            new InviteMemberRequest { Email = "spouse@example.com" });
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<HouseholdInviteDto>();
        invite!.Token.Should().NotBeNullOrEmpty();
        invite.Status.Should().Be("Pending");

        // Spouse signs in and accepts.
        var spouse = await _factory.CreateAuthenticatedClientAsync("spouse@example.com");
        var acceptResponse = await spouse.PostAsync($"/api/v1/invites/{invite.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Both members now visible to the spouse.
        var current = await spouse.GetFromJsonAsync<HouseholdDto>("/api/v1/households/current");
        current!.Members.Should().HaveCount(2);
        current.Members.Select(m => m.Role).Should().Contain(new[] { "Owner", "Member" });
    }

    [Fact]
    public async Task Owner_can_remove_an_invited_member()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync("owner2@example.com");
        var created = await (await owner.PostAsJsonAsync("/api/v1/households",
            new CreateHouseholdRequest { Name = "Family" })).Content.ReadFromJsonAsync<HouseholdDto>();

        var invite = await (await owner.PostAsJsonAsync($"/api/v1/households/{created!.Id}/invites",
            new InviteMemberRequest { Email = "spouse2@example.com" })).Content.ReadFromJsonAsync<HouseholdInviteDto>();

        var spouse = await _factory.CreateAuthenticatedClientAsync("spouse2@example.com");
        await spouse.PostAsync($"/api/v1/invites/{invite!.Token}/accept", null);

        var current = await owner.GetFromJsonAsync<HouseholdDto>("/api/v1/households/current");
        var spouseMember = current!.Members.Single(m => m.Role == "Member");

        var removeResponse = await owner.DeleteAsync($"/api/v1/households/{created.Id}/members/{spouseMember.Id}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await owner.GetFromJsonAsync<HouseholdDto>("/api/v1/households/current");
        after!.Members.Should().ContainSingle().Which.Role.Should().Be("Owner");
    }

    [Fact]
    public async Task Owner_cannot_be_removed()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var created = await (await owner.PostAsJsonAsync("/api/v1/households",
            new CreateHouseholdRequest { Name = "Family" })).Content.ReadFromJsonAsync<HouseholdDto>();
        var ownerMember = created!.Members.Single();

        var response = await owner.DeleteAsync($"/api/v1/households/{created.Id}/members/{ownerMember.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Non_owner_cannot_invite()
    {
        // Owner creates a household and invites the spouse, who accepts (becomes a plain Member).
        var owner = await _factory.CreateAuthenticatedClientAsync("owner3@example.com");
        var created = await (await owner.PostAsJsonAsync("/api/v1/households",
            new CreateHouseholdRequest { Name = "Family" })).Content.ReadFromJsonAsync<HouseholdDto>();
        var invite = await (await owner.PostAsJsonAsync($"/api/v1/households/{created!.Id}/invites",
            new InviteMemberRequest { Email = "spouse3@example.com" })).Content.ReadFromJsonAsync<HouseholdInviteDto>();
        var spouse = await _factory.CreateAuthenticatedClientAsync("spouse3@example.com");
        await spouse.PostAsync($"/api/v1/invites/{invite!.Token}/accept", null);

        // The plain member tries to invite someone else.
        var response = await spouse.PostAsJsonAsync($"/api/v1/households/{created.Id}/invites",
            new InviteMemberRequest { Email = "outsider@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Current_household_requires_authentication()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/v1/households/current");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Current_household_returns_404_when_user_has_none()
    {
        var loner = await _factory.CreateAuthenticatedClientAsync();
        var response = await loner.GetAsync("/api/v1/households/current");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

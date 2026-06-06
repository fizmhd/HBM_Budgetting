using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Accounts;
using BudgetTracker.Shared.DTOs.Households;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the Accounts API (TASK 2.2): CRUD, validation, and the owner/household
/// visibility rule.
/// </summary>
public class AccountEndpointTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;

    public AccountEndpointTests(AuthenticatedWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private static CreateAccountRequest Bank(string name, bool shared = false) => new()
    {
        Name = name,
        Type = "Bank",
        CurrencyCode = "SEK",
        OpeningBalance = 1000m,
        IsShared = shared
    };

    [Fact]
    public async Task Listing_requires_authentication()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/v1/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_then_list_returns_the_account_with_opening_balance()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var created = await (await client.PostAsJsonAsync("/api/v1/accounts", Bank("Checking")))
            .Content.ReadFromJsonAsync<AccountDto>();
        created!.CurrencyCode.Should().Be("SEK");
        created.Balance.Should().Be(1000m);
        created.IsShared.Should().BeFalse();

        var list = await client.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        list.Should().ContainSingle().Which.Name.Should().Be("Checking");
    }

    [Fact]
    public async Task Credit_limit_is_rejected_for_non_card_types()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var request = Bank("Wallet");
        request.CreditLimit = 5000m;

        var response = await client.PostAsJsonAsync("/api/v1/accounts", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Credit_limit_is_kept_for_credit_cards()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var card = await (await client.PostAsJsonAsync("/api/v1/accounts", new CreateAccountRequest
        {
            Name = "Visa",
            Type = "CreditCard",
            CurrencyCode = "SEK",
            CreditLimit = 20000m
        })).Content.ReadFromJsonAsync<AccountDto>();

        card!.Type.Should().Be("CreditCard");
        card.CreditLimit.Should().Be(20000m);
    }

    [Fact]
    public async Task Unsupported_currency_is_rejected()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var request = Bank("Bad");
        request.CurrencyCode = "XYZ";

        var response = await client.PostAsJsonAsync("/api/v1/accounts", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Archive_toggles_the_archived_flag()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await (await client.PostAsJsonAsync("/api/v1/accounts", Bank("Checking")))
            .Content.ReadFromJsonAsync<AccountDto>();

        var archived = await (await client.PostAsync($"/api/v1/accounts/{account!.Id}/archive", null))
            .Content.ReadFromJsonAsync<AccountDto>();
        archived!.IsArchived.Should().BeTrue();

        var unarchived = await (await client.PostAsync($"/api/v1/accounts/{account.Id}/archive", null))
            .Content.ReadFromJsonAsync<AccountDto>();
        unarchived!.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task Household_shared_account_is_visible_to_spouse_but_individual_is_not()
    {
        // Owner sets up a household and invites the spouse.
        var owner = await _factory.CreateAuthenticatedClientAsync("owner@example.com");
        var household = await (await owner.PostAsJsonAsync("/api/v1/households",
            new CreateHouseholdRequest { Name = "Family" })).Content.ReadFromJsonAsync<HouseholdDto>();
        var invite = await (await owner.PostAsJsonAsync($"/api/v1/households/{household!.Id}/invites",
            new InviteMemberRequest { Email = "spouse@example.com" })).Content.ReadFromJsonAsync<HouseholdInviteDto>();
        var spouse = await _factory.CreateAuthenticatedClientAsync("spouse@example.com");
        await spouse.PostAsync($"/api/v1/invites/{invite!.Token}/accept", null);

        // Owner creates one shared and one private account.
        await owner.PostAsJsonAsync("/api/v1/accounts", Bank("Joint", shared: true));
        await owner.PostAsJsonAsync("/api/v1/accounts", Bank("Owner Private", shared: false));

        // Spouse sees only the shared one.
        var spouseList = await spouse.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        spouseList.Should().ContainSingle();
        spouseList![0].Name.Should().Be("Joint");
        spouseList[0].IsShared.Should().BeTrue();

        // Owner sees both.
        var ownerList = await owner.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        ownerList.Should().HaveCount(2);
    }

    [Fact]
    public async Task Another_users_individual_account_cannot_be_fetched()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("alice@example.com");
        var aliceAccount = await (await alice.PostAsJsonAsync("/api/v1/accounts", Bank("Alice")))
            .Content.ReadFromJsonAsync<AccountDto>();

        var bob = await _factory.CreateAuthenticatedClientAsync("bob@example.com");
        var response = await bob.GetAsync($"/api/v1/accounts/{aliceAccount!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Sharing_without_a_household_is_rejected()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/accounts", Bank("Joint", shared: true));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

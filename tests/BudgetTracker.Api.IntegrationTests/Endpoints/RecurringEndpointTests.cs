using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Accounts;
using BudgetTracker.Shared.DTOs.Categories;
using BudgetTracker.Shared.DTOs.Recurring;
using BudgetTracker.Shared.DTOs.Transactions;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the Recurring API (Sprint 5): generation in both modes, idempotency,
/// pause/skip/confirm lifecycle, kind filter, and the category-in-use deletion rule.
/// </summary>
public class RecurringEndpointTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;

    // Rules start "today" so the manual generate (asOf = today) produces exactly one occurrence.
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public RecurringEndpointTests(AuthenticatedWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private static async Task<AccountDto> CreateAccountAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new CreateAccountRequest
        {
            Name = "Checking", Type = "Bank", CurrencyCode = "SEK", OpeningBalance = 0m
        });
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    private static async Task<CategoryDto> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/categories", new CreateCategoryRequest { Name = name });
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private static CreateRecurringRuleRequest Rule(Guid categoryId, Guid? accountId, string mode,
        decimal amount = 100m, bool subscription = false, string type = "Expense") => new()
    {
        Name = subscription ? "Netflix" : "Rent",
        Type = type,
        AccountId = accountId,
        CategoryId = categoryId,
        Amount = amount,
        Frequency = "Monthly",
        Interval = 1,
        StartDate = Today,
        GenerationMode = mode,
        IsSubscription = subscription
    };

    private static async Task<int> GenerateAsync(HttpClient client)
    {
        var result = await (await client.PostAsync("/api/v1/recurring/generate", null))
            .Content.ReadFromJsonAsync<RecurringGenerationResultDto>();
        return result!.Generated;
    }

    [Fact]
    public async Task Create_rule_sets_next_due_to_start()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Housing");

        var created = await (await client.PostAsJsonAsync("/api/v1/recurring",
            Rule(category.Id, null, "AutoPost"))).Content.ReadFromJsonAsync<RecurringRuleDto>();

        created!.NextDueDate.Should().Be(Today);
        created.Status.Should().Be("Active");
    }

    [Fact]
    public async Task AutoPost_generates_a_transaction_and_is_idempotent()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client);
        var category = await CreateCategoryAsync(client, "Housing");
        await client.PostAsJsonAsync("/api/v1/recurring", Rule(category.Id, account.Id, "AutoPost", amount: 1200m));

        var firstRun = await GenerateAsync(client);
        firstRun.Should().Be(1);

        var afterFirst = await client.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions");
        afterFirst!.TotalCount.Should().Be(1);
        afterFirst.Items[0].Amount.Should().Be(1200m);
        afterFirst.Items[0].Type.Should().Be("Expense");

        // Re-running generates nothing new.
        var secondRun = await GenerateAsync(client);
        secondRun.Should().Be(0);

        var afterSecond = await client.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions");
        afterSecond!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task PendingConfirm_creates_pending_then_confirm_posts_transaction()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client);
        var category = await CreateCategoryAsync(client, "Utilities");
        await client.PostAsJsonAsync("/api/v1/recurring", Rule(category.Id, account.Id, "PendingConfirm", amount: 300m));

        await GenerateAsync(client);

        // No transaction yet; one pending occurrence.
        (await client.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions"))!.TotalCount.Should().Be(0);
        var pending = await client.GetFromJsonAsync<List<RecurringOccurrenceDto>>("/api/v1/recurring/occurrences/pending");
        pending.Should().ContainSingle();

        // Confirm posts the transaction.
        var confirmed = await (await client.PostAsync($"/api/v1/recurring/occurrences/{pending![0].Id}/confirm", null))
            .Content.ReadFromJsonAsync<RecurringOccurrenceDto>();
        confirmed!.Status.Should().Be("Posted");
        confirmed.GeneratedTransactionId.Should().NotBeNull();

        (await client.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions"))!.TotalCount.Should().Be(1);
        (await client.GetFromJsonAsync<List<RecurringOccurrenceDto>>("/api/v1/recurring/occurrences/pending"))!
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Paused_rule_does_not_generate()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Housing");
        var rule = await (await client.PostAsJsonAsync("/api/v1/recurring",
            Rule(category.Id, null, "AutoPost"))).Content.ReadFromJsonAsync<RecurringRuleDto>();

        var paused = await (await client.PostAsync($"/api/v1/recurring/{rule!.Id}/pause", null))
            .Content.ReadFromJsonAsync<RecurringRuleDto>();
        paused!.Status.Should().Be("Paused");
        paused.PausedAt.Should().NotBeNull();

        (await GenerateAsync(client)).Should().Be(0);

        // Resume re-enables generation.
        await client.PostAsync($"/api/v1/recurring/{rule.Id}/resume", null);
        (await GenerateAsync(client)).Should().Be(1);
    }

    [Fact]
    public async Task Skip_requires_a_reason_and_marks_skipped()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Utilities");
        await client.PostAsJsonAsync("/api/v1/recurring", Rule(category.Id, null, "PendingConfirm"));
        await GenerateAsync(client);
        var pending = await client.GetFromJsonAsync<List<RecurringOccurrenceDto>>("/api/v1/recurring/occurrences/pending");

        // No reason → rejected.
        var noReason = await client.PostAsJsonAsync($"/api/v1/recurring/occurrences/{pending![0].Id}/skip",
            new SkipOccurrenceRequest { Reason = "" });
        noReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // With reason → skipped, removed from the pending queue.
        var skipped = await (await client.PostAsJsonAsync($"/api/v1/recurring/occurrences/{pending[0].Id}/skip",
            new SkipOccurrenceRequest { Reason = "paid manually" })).Content.ReadFromJsonAsync<RecurringOccurrenceDto>();
        skipped!.Status.Should().Be("Skipped");
        skipped.SkipReason.Should().Be("paid manually");

        (await client.GetFromJsonAsync<List<RecurringOccurrenceDto>>("/api/v1/recurring/occurrences/pending"))!
            .Should().BeEmpty();
    }

    [Fact]
    public async Task List_filters_by_kind()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Subs");
        await client.PostAsJsonAsync("/api/v1/recurring", Rule(category.Id, null, "AutoPost", subscription: true));
        await client.PostAsJsonAsync("/api/v1/recurring", Rule(category.Id, null, "AutoPost", subscription: false));

        var subs = await client.GetFromJsonAsync<List<RecurringRuleDto>>("/api/v1/recurring?kind=subscription");
        subs!.Should().ContainSingle().Which.IsSubscription.Should().BeTrue();

        var expenses = await client.GetFromJsonAsync<List<RecurringRuleDto>>("/api/v1/recurring?kind=expense");
        expenses!.Should().ContainSingle().Which.IsSubscription.Should().BeFalse();
    }

    [Fact]
    public async Task Deleting_a_category_used_by_a_rule_is_blocked()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Housing");
        await client.PostAsJsonAsync("/api/v1/recurring", Rule(category.Id, null, "AutoPost"));

        var response = await client.DeleteAsync($"/api/v1/categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

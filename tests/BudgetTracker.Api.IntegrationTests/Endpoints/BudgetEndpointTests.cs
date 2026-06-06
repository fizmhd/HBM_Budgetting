using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Accounts;
using BudgetTracker.Shared.DTOs.Budgets;
using BudgetTracker.Shared.DTOs.Categories;
using BudgetTracker.Shared.DTOs.Transactions;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the Budgets API (Sprint 6): CRUD, spent-vs-budget progress, status
/// boundaries, validation, and the category-in-use deletion rule.
/// </summary>
public class BudgetEndpointTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;

    public BudgetEndpointTests(AuthenticatedWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private static readonly DateOnly MonthStart = new(2026, 6, 1);
    private static readonly DateOnly MonthEnd = new(2026, 6, 30);

    private static async Task<AccountDto> CreateAccountAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new CreateAccountRequest
        {
            Name = "Checking",
            Type = "Bank",
            CurrencyCode = "SEK",
            OpeningBalance = 0m
        });
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    private static async Task<CategoryDto> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/categories", new CreateCategoryRequest { Name = name });
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private static async Task SpendAsync(HttpClient client, Guid accountId, Guid categoryId, decimal amount,
        DateOnly date)
    {
        await client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = "Expense",
            AccountId = accountId,
            Date = date,
            Amount = amount,
            Splits = new() { new TransactionSplitInput { CategoryId = categoryId, Amount = amount } }
        });
    }

    private static CreateBudgetRequest Budget(Guid categoryId, decimal amount, int threshold = 80) => new()
    {
        CategoryId = categoryId,
        PeriodType = "Month",
        PeriodStart = MonthStart,
        PeriodEnd = MonthEnd,
        Amount = amount,
        AlertThresholdPercent = threshold
    };

    [Fact]
    public async Task Create_budget_starts_with_zero_spent()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Groceries");

        var created = await (await client.PostAsJsonAsync("/api/v1/budgets", Budget(category.Id, 1000m)))
            .Content.ReadFromJsonAsync<BudgetDto>();

        created!.CategoryName.Should().Be("Groceries");
        created.Spent.Should().Be(0m);
        created.Remaining.Should().Be(1000m);
        created.Status.Should().Be("Ok");
    }

    [Fact]
    public async Task Spent_matches_expenses_in_the_period()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client);
        var category = await CreateCategoryAsync(client, "Groceries");

        await SpendAsync(client, account.Id, category.Id, 250m, new DateOnly(2026, 6, 5));
        await SpendAsync(client, account.Id, category.Id, 150m, new DateOnly(2026, 6, 20));
        // Outside the period — must not count.
        await SpendAsync(client, account.Id, category.Id, 999m, new DateOnly(2026, 7, 1));

        await client.PostAsJsonAsync("/api/v1/budgets", Budget(category.Id, 1000m));

        var budgets = await client.GetFromJsonAsync<List<BudgetDto>>("/api/v1/budgets");
        var b = budgets!.Single();
        b.Spent.Should().Be(400m);
        b.Remaining.Should().Be(600m);
        b.PercentUsed.Should().Be(40m);
        b.Status.Should().Be("Ok");
    }

    [Fact]
    public async Task Status_is_warning_at_threshold_and_exceeded_at_limit()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client);
        var category = await CreateCategoryAsync(client, "Dining");

        var budget = await (await client.PostAsJsonAsync("/api/v1/budgets", Budget(category.Id, 100m, threshold: 80)))
            .Content.ReadFromJsonAsync<BudgetDto>();

        // Spend to exactly the threshold → Warning.
        await SpendAsync(client, account.Id, category.Id, 80m, new DateOnly(2026, 6, 10));
        var warning = (await client.GetFromJsonAsync<List<BudgetDto>>("/api/v1/budgets"))!.Single();
        warning.PercentUsed.Should().Be(80m);
        warning.Status.Should().Be("Warning");

        // Spend up to the limit → Exceeded.
        await SpendAsync(client, account.Id, category.Id, 20m, new DateOnly(2026, 6, 11));
        var exceeded = (await client.GetFromJsonAsync<BudgetDto>($"/api/v1/budgets/{budget!.Id}"))!;
        exceeded.PercentUsed.Should().Be(100m);
        exceeded.Status.Should().Be("Exceeded");
        exceeded.Remaining.Should().Be(0m);
    }

    [Fact]
    public async Task List_filters_to_budgets_overlapping_the_window()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Travel");

        // A June budget and a separate August budget.
        await client.PostAsJsonAsync("/api/v1/budgets", Budget(category.Id, 500m));
        await client.PostAsJsonAsync("/api/v1/budgets", new CreateBudgetRequest
        {
            CategoryId = category.Id,
            PeriodType = "Month",
            PeriodStart = new DateOnly(2026, 8, 1),
            PeriodEnd = new DateOnly(2026, 8, 31),
            Amount = 700m
        });

        var june = await client.GetFromJsonAsync<List<BudgetDto>>("/api/v1/budgets?from=2026-06-01&to=2026-06-30");
        june!.Should().ContainSingle().Which.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task Invalid_amount_or_period_is_rejected()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Groceries");

        var zeroAmount = await client.PostAsJsonAsync("/api/v1/budgets", Budget(category.Id, 0m));
        zeroAmount.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var badPeriod = await client.PostAsJsonAsync("/api/v1/budgets", new CreateBudgetRequest
        {
            CategoryId = category.Id,
            PeriodType = "Month",
            PeriodStart = new DateOnly(2026, 6, 30),
            PeriodEnd = new DateOnly(2026, 6, 1),
            Amount = 100m
        });
        badPeriod.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deleting_a_category_used_by_a_budget_is_blocked()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Groceries");
        await client.PostAsJsonAsync("/api/v1/budgets", Budget(category.Id, 1000m));

        var response = await client.DeleteAsync($"/api/v1/categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_and_delete_a_budget()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Groceries");

        var created = await (await client.PostAsJsonAsync("/api/v1/budgets", Budget(category.Id, 1000m)))
            .Content.ReadFromJsonAsync<BudgetDto>();

        var updated = await (await client.PutAsJsonAsync($"/api/v1/budgets/{created!.Id}", new UpdateBudgetRequest
        {
            CategoryId = category.Id,
            PeriodType = "Month",
            PeriodStart = MonthStart,
            PeriodEnd = MonthEnd,
            Amount = 1500m,
            AlertThresholdPercent = 90
        })).Content.ReadFromJsonAsync<BudgetDto>();
        updated!.Amount.Should().Be(1500m);
        updated.AlertThresholdPercent.Should().Be(90);

        var delete = await client.DeleteAsync($"/api/v1/budgets/{created.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await client.GetFromJsonAsync<List<BudgetDto>>("/api/v1/budgets");
        afterDelete!.Should().BeEmpty();
    }
}

using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Accounts;
using BudgetTracker.Shared.DTOs.Budgets;
using BudgetTracker.Shared.DTOs.Categories;
using BudgetTracker.Shared.DTOs.Dashboard;
using BudgetTracker.Shared.DTOs.Households;
using BudgetTracker.Shared.DTOs.Transactions;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the dashboard aggregation API (Sprint 7 / TASK 7.1): reconciliation with the
/// transactions list, transfer neutrality, scope (individual vs household), and the budget summary.
/// </summary>
public class DashboardEndpointTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;

    public DashboardEndpointTests(AuthenticatedWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private static async Task<AccountDto> CreateAccountAsync(HttpClient client, string name = "Checking",
        decimal opening = 0m)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new CreateAccountRequest
        {
            Name = name,
            Type = "Bank",
            CurrencyCode = "SEK",
            OpeningBalance = opening
        });
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    private static async Task<CategoryDto> CreateCategoryAsync(HttpClient client, string name, bool shared = false)
    {
        var response = await client.PostAsJsonAsync("/api/v1/categories",
            new CreateCategoryRequest { Name = name, IsShared = shared });
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private static Task PostTxnAsync(HttpClient client, string type, decimal amount, Guid categoryId,
        DateOnly date, Guid? accountId = null, bool shared = false) =>
        client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = type,
            AccountId = accountId,
            Date = date,
            Amount = amount,
            IsShared = shared,
            Splits = new() { new TransactionSplitInput { CategoryId = categoryId, Amount = amount } }
        });

    [Fact]
    public async Task Totals_and_breakdown_reconcile_with_the_months_transactions()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client);
        var salary = await CreateCategoryAsync(client, "Salary");
        var groceries = await CreateCategoryAsync(client, "Groceries");
        var dining = await CreateCategoryAsync(client, "Dining");

        await PostTxnAsync(client, "Income", 2000m, salary.Id, new DateOnly(2026, 6, 1), account.Id);
        await PostTxnAsync(client, "Expense", 300m, groceries.Id, new DateOnly(2026, 6, 5), account.Id);
        await PostTxnAsync(client, "Expense", 100m, dining.Id, new DateOnly(2026, 6, 10), account.Id);
        // Different month — must be excluded.
        await PostTxnAsync(client, "Expense", 999m, groceries.Id, new DateOnly(2026, 7, 1), account.Id);

        var dash = await client.GetFromJsonAsync<MonthlyDashboardDto>("/api/v1/dashboard/monthly?month=2026-06");

        dash!.TotalIncome.Should().Be(2000m);
        dash.TotalExpenses.Should().Be(400m);
        dash.NetBalance.Should().Be(1600m);

        dash.ByCategory.Should().HaveCount(2);
        dash.ByCategory[0].Name.Should().Be("Groceries"); // largest first
        dash.ByCategory[0].Amount.Should().Be(300m);
        dash.ByCategory[0].Percent.Should().Be(75m);
        dash.ByCategory[1].Name.Should().Be("Dining");
        dash.ByCategory[1].Amount.Should().Be(100m);
    }

    [Fact]
    public async Task Transfers_do_not_affect_income_or_expenses()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var from = await CreateAccountAsync(client, "Checking", opening: 1000m);
        var to = await CreateAccountAsync(client, "Savings");

        await client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = "Transfer",
            AccountId = from.Id,
            CounterAccountId = to.Id,
            Date = new DateOnly(2026, 6, 3),
            Amount = 250m
        });

        var dash = await client.GetFromJsonAsync<MonthlyDashboardDto>("/api/v1/dashboard/monthly?month=2026-06");
        dash!.TotalIncome.Should().Be(0m);
        dash.TotalExpenses.Should().Be(0m);
        dash.NetBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Account_less_expense_counts_toward_dashboard_totals()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var category = await CreateCategoryAsync(client, "Cash spend");

        await PostTxnAsync(client, "Expense", 60m, category.Id, new DateOnly(2026, 6, 7)); // no account

        var dash = await client.GetFromJsonAsync<MonthlyDashboardDto>("/api/v1/dashboard/monthly?month=2026-06");
        dash!.TotalExpenses.Should().Be(60m);
    }

    [Fact]
    public async Task Individual_scope_excludes_other_members_shared_data()
    {
        // Owner sets up a household and invites a spouse who accepts.
        var owner = await _factory.CreateAuthenticatedClientAsync("owner-dash@example.com");
        var household = await (await owner.PostAsJsonAsync("/api/v1/households",
            new CreateHouseholdRequest { Name = "Family" })).Content.ReadFromJsonAsync<HouseholdDto>();
        var invite = await (await owner.PostAsJsonAsync($"/api/v1/households/{household!.Id}/invites",
            new InviteMemberRequest { Email = "spouse-dash@example.com" })).Content.ReadFromJsonAsync<HouseholdInviteDto>();

        var spouse = await _factory.CreateAuthenticatedClientAsync("spouse-dash@example.com");
        await spouse.PostAsync($"/api/v1/invites/{invite!.Token}/accept", null);

        // Spouse logs a household-shared expense.
        var spouseCategory = await CreateCategoryAsync(spouse, "Shared groceries", shared: true);
        await PostTxnAsync(spouse, "Expense", 500m, spouseCategory.Id, new DateOnly(2026, 6, 4), shared: true);

        // Owner's household view includes the spouse's shared expense...
        var household_view = await owner.GetFromJsonAsync<MonthlyDashboardDto>(
            "/api/v1/dashboard/monthly?month=2026-06&scope=household");
        household_view!.TotalExpenses.Should().Be(500m);

        // ...but the owner's individual view does not (the owner logged nothing).
        var individual_view = await owner.GetFromJsonAsync<MonthlyDashboardDto>(
            "/api/v1/dashboard/monthly?month=2026-06&scope=individual");
        individual_view!.TotalExpenses.Should().Be(0m);
    }

    [Fact]
    public async Task Budget_summary_counts_statuses()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client);
        var groceries = await CreateCategoryAsync(client, "Groceries");

        // A 1000 budget with 950 spent → Warning (>= 80%).
        await client.PostAsJsonAsync("/api/v1/budgets", new CreateBudgetRequest
        {
            CategoryId = groceries.Id,
            PeriodType = "Month",
            PeriodStart = new DateOnly(2026, 6, 1),
            PeriodEnd = new DateOnly(2026, 6, 30),
            Amount = 1000m,
            AlertThresholdPercent = 80
        });
        await PostTxnAsync(client, "Expense", 950m, groceries.Id, new DateOnly(2026, 6, 9), account.Id);

        var dash = await client.GetFromJsonAsync<MonthlyDashboardDto>("/api/v1/dashboard/monthly?month=2026-06");
        dash!.BudgetSummary.Total.Should().Be(1);
        dash.BudgetSummary.Warning.Should().Be(1);
        dash.BudgetSummary.TotalBudgeted.Should().Be(1000m);
        dash.BudgetSummary.TotalSpent.Should().Be(950m);
    }
}

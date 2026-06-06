using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Accounts;
using BudgetTracker.Shared.DTOs.Budgets;
using BudgetTracker.Shared.DTOs.Categories;
using BudgetTracker.Shared.DTOs.Dashboard;
using BudgetTracker.Shared.DTOs.Transactions;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// The MVP core-flow end-to-end test (TASK 7.4): create account → add categories → record
/// income/expense/transfer → set a budget → read the dashboard, asserting every layer reconciles.
/// </summary>
public class EndToEndFlowTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;

    public EndToEndFlowTests(AuthenticatedWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Full_monthly_loop_reconciles_balances_budget_and_dashboard()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // 1) Accounts.
        var checking = await (await client.PostAsJsonAsync("/api/v1/accounts", new CreateAccountRequest
        {
            Name = "Checking", Type = "Bank", CurrencyCode = "SEK", OpeningBalance = 5000m
        })).Content.ReadFromJsonAsync<AccountDto>();
        var savings = await (await client.PostAsJsonAsync("/api/v1/accounts", new CreateAccountRequest
        {
            Name = "Savings", Type = "Savings", CurrencyCode = "SEK", OpeningBalance = 0m
        })).Content.ReadFromJsonAsync<AccountDto>();

        // 2) Categories.
        var salary = await (await client.PostAsJsonAsync("/api/v1/categories",
            new CreateCategoryRequest { Name = "Salary" })).Content.ReadFromJsonAsync<CategoryDto>();
        var groceries = await (await client.PostAsJsonAsync("/api/v1/categories",
            new CreateCategoryRequest { Name = "Groceries" })).Content.ReadFromJsonAsync<CategoryDto>();

        // 3) Transactions: salary in, groceries out, and a transfer to savings.
        await client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = "Income", AccountId = checking!.Id, Date = new DateOnly(2026, 6, 1), Amount = 30000m,
            Splits = new() { new TransactionSplitInput { CategoryId = salary!.Id, Amount = 30000m } }
        });
        await client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = "Expense", AccountId = checking.Id, Date = new DateOnly(2026, 6, 8), Amount = 4200m,
            Splits = new() { new TransactionSplitInput { CategoryId = groceries!.Id, Amount = 4200m } }
        });
        await client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = "Transfer", AccountId = checking.Id, CounterAccountId = savings!.Id,
            Date = new DateOnly(2026, 6, 10), Amount = 10000m
        });

        // 4) Budget for groceries.
        var budget = await (await client.PostAsJsonAsync("/api/v1/budgets", new CreateBudgetRequest
        {
            CategoryId = groceries.Id, PeriodType = "Month",
            PeriodStart = new DateOnly(2026, 6, 1), PeriodEnd = new DateOnly(2026, 6, 30),
            Amount = 5000m, AlertThresholdPercent = 80
        })).Content.ReadFromJsonAsync<BudgetDto>();

        // Balances: checking = 5000 + 30000 − 4200 − 10000 = 20800; savings = 10000.
        var checkingAfter = await client.GetFromJsonAsync<AccountDto>($"/api/v1/accounts/{checking.Id}");
        var savingsAfter = await client.GetFromJsonAsync<AccountDto>($"/api/v1/accounts/{savings.Id}");
        checkingAfter!.Balance.Should().Be(20800m);
        savingsAfter!.Balance.Should().Be(10000m);

        // Budget: 4200 / 5000 = 84% → Warning.
        budget!.Spent.Should().Be(4200m);
        budget.PercentUsed.Should().Be(84m);
        budget.Status.Should().Be("Warning");

        // Dashboard ties it together (transfer excluded from income/expenses).
        var dash = await client.GetFromJsonAsync<MonthlyDashboardDto>("/api/v1/dashboard/monthly?month=2026-06");
        dash!.TotalIncome.Should().Be(30000m);
        dash.TotalExpenses.Should().Be(4200m);
        dash.NetBalance.Should().Be(25800m);
        dash.ByCategory.Should().ContainSingle().Which.Name.Should().Be("Groceries");
        dash.BudgetSummary.Warning.Should().Be(1);
        dash.TopAccounts.Should().HaveCount(2);
        dash.TopAccounts[0].Name.Should().Be("Checking"); // highest balance first

        // The transactions list reconciles with the dashboard for the same month.
        var list = await client.GetFromJsonAsync<TransactionListResponse>(
            "/api/v1/transactions?from=2026-06-01&to=2026-06-30");
        list!.Items.Sum(t => t.Type == "Income" ? t.Amount : 0).Should().Be(dash.TotalIncome);
        list.Items.Sum(t => t.Type == "Expense" ? t.Amount : 0).Should().Be(dash.TotalExpenses);
    }
}

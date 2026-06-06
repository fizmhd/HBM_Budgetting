using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Accounts;
using BudgetTracker.Shared.DTOs.Categories;
using BudgetTracker.Shared.DTOs.Transactions;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the Transactions API (Sprint 4): CRUD, split invariants, transfers,
/// filtering, and derived balances.
/// </summary>
public class TransactionEndpointTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;

    public TransactionEndpointTests(AuthenticatedWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private static async Task<AccountDto> CreateAccountAsync(HttpClient client, string name, decimal opening = 0m)
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

    private static async Task<CategoryDto> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/categories", new CreateCategoryRequest { Name = name });
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    [Fact]
    public async Task Create_expense_with_split_then_list_and_balance()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client, "Checking", opening: 1000m);
        var category = await CreateCategoryAsync(client, "Groceries");

        var request = new CreateTransactionRequest
        {
            Type = "Expense",
            AccountId = account.Id,
            Date = new DateOnly(2026, 6, 1),
            Amount = 250m,
            Description = "Weekly shop",
            Splits = new() { new TransactionSplitInput { CategoryId = category.Id, Amount = 250m } }
        };

        var created = await (await client.PostAsJsonAsync("/api/v1/transactions", request))
            .Content.ReadFromJsonAsync<TransactionDto>();
        created!.Splits.Should().ContainSingle().Which.CategoryName.Should().Be("Groceries");

        var list = await client.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions");
        list!.TotalCount.Should().Be(1);

        // Opening 1000 − 250 expense = 750.
        var refreshed = await client.GetFromJsonAsync<AccountDto>($"/api/v1/accounts/{account.Id}");
        refreshed!.Balance.Should().Be(750m);
    }

    [Fact]
    public async Task Split_sum_mismatch_is_rejected()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client, "Checking");
        var category = await CreateCategoryAsync(client, "Groceries");

        var request = new CreateTransactionRequest
        {
            Type = "Expense",
            AccountId = account.Id,
            Date = new DateOnly(2026, 6, 1),
            Amount = 100m,
            Splits = new() { new TransactionSplitInput { CategoryId = category.Id, Amount = 80m } }
        };

        var response = await client.PostAsJsonAsync("/api/v1/transactions", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transfer_moves_money_and_nets_zero()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var from = await CreateAccountAsync(client, "Checking", opening: 1000m);
        var to = await CreateAccountAsync(client, "Savings", opening: 500m);

        var request = new CreateTransactionRequest
        {
            Type = "Transfer",
            AccountId = from.Id,
            CounterAccountId = to.Id,
            Date = new DateOnly(2026, 6, 1),
            Amount = 300m
        };

        var response = await client.PostAsJsonAsync("/api/v1/transactions", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fromRefreshed = await client.GetFromJsonAsync<AccountDto>($"/api/v1/accounts/{from.Id}");
        var toRefreshed = await client.GetFromJsonAsync<AccountDto>($"/api/v1/accounts/{to.Id}");
        fromRefreshed!.Balance.Should().Be(700m);
        toRefreshed!.Balance.Should().Be(800m);
    }

    [Fact]
    public async Task Transfer_to_same_account_is_rejected()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client, "Checking");

        var request = new CreateTransactionRequest
        {
            Type = "Transfer",
            AccountId = account.Id,
            CounterAccountId = account.Id,
            Date = new DateOnly(2026, 6, 1),
            Amount = 100m
        };

        var response = await client.PostAsJsonAsync("/api/v1/transactions", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deleting_a_category_used_by_a_transaction_is_blocked()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client, "Checking");
        var category = await CreateCategoryAsync(client, "Groceries");

        await client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = "Expense",
            AccountId = account.Id,
            Date = new DateOnly(2026, 6, 1),
            Amount = 50m,
            Splits = new() { new TransactionSplitInput { CategoryId = category.Id, Amount = 50m } }
        });

        var response = await client.DeleteAsync($"/api/v1/categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task List_filters_by_type()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client, "Checking", opening: 1000m);
        var to = await CreateAccountAsync(client, "Savings");
        var category = await CreateCategoryAsync(client, "Salary");

        await client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = "Income",
            AccountId = account.Id,
            Date = new DateOnly(2026, 6, 1),
            Amount = 2000m,
            Splits = new() { new TransactionSplitInput { CategoryId = category.Id, Amount = 2000m } }
        });
        await client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = "Transfer",
            AccountId = account.Id,
            CounterAccountId = to.Id,
            Date = new DateOnly(2026, 6, 2),
            Amount = 100m
        });

        var income = await client.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions?type=Income");
        income!.TotalCount.Should().Be(1);
        income.Items[0].Type.Should().Be("Income");
    }

    [Fact]
    public async Task Update_recategorises_and_tags_a_transaction()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client, "Checking", opening: 1000m);
        var groceries = await CreateCategoryAsync(client, "Groceries");
        var dining = await CreateCategoryAsync(client, "Dining");

        var created = await (await client.PostAsJsonAsync("/api/v1/transactions", new CreateTransactionRequest
        {
            Type = "Expense",
            AccountId = account.Id,
            Date = new DateOnly(2026, 6, 1),
            Amount = 100m,
            Splits = new() { new TransactionSplitInput { CategoryId = groceries.Id, Amount = 100m } }
        })).Content.ReadFromJsonAsync<TransactionDto>();

        var updated = await (await client.PutAsJsonAsync($"/api/v1/transactions/{created!.Id}",
            new UpdateTransactionRequest
            {
                Type = "Expense",
                AccountId = account.Id,
                Date = new DateOnly(2026, 6, 1),
                Amount = 100m,
                Tags = new() { "dining-out" },
                Splits = new() { new TransactionSplitInput { CategoryId = dining.Id, Amount = 100m } }
            })).Content.ReadFromJsonAsync<TransactionDto>();

        updated!.Splits.Should().ContainSingle().Which.CategoryName.Should().Be("Dining");
        updated.Tags.Should().Contain("dining-out");
    }
}

using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Accounts;
using BudgetTracker.Shared.DTOs.Categories;
using BudgetTracker.Shared.DTOs.Payslips;
using BudgetTracker.Shared.DTOs.Transactions;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the Payslip API (Sprint 8): a real lönespecifikation reconciles to net,
/// the summary/YTD auto-compute from line items, posting creates the income transaction, and the
/// personnummer is encrypted (never returned in clear, only masked).
/// </summary>
public class PayslipEndpointTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;

    public PayslipEndpointTests(AuthenticatedWebApplicationFactory factory)
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

    // The spec's worked example: Grundlön 55 000, Bilförmån 2 000, Preliminärskatt 13 443 → net 41 557.
    private static CreatePayslipRequest SamplePayslip(DateOnly payDate, string? personnummer = "19900101-1234") => new()
    {
        Country = "Sweden",
        EmployerName = "Acme AB",
        EmployeeName = "Test Person",
        Personnummer = personnummer,
        PayPeriodStart = new DateOnly(payDate.Year, payDate.Month, 1),
        PayPeriodEnd = payDate,
        PayDate = payDate,
        CurrencyCode = "SEK",
        DeclaredNet = 41_557m,
        LineItems = new List<PayslipLineItemInput>
        {
            new() { Type = "Earning", Label = "Grundlön", Amount = 55_000m, SortOrder = 0 },
            new() { Type = "Benefit", Label = "Bilförmån", Amount = 2_000m, SortOrder = 1 },
            new() { Type = "Tax", Label = "Preliminärskatt", Amount = 13_443m, SortOrder = 2 }
        },
        LeaveBalances = new List<PayslipLeaveBalanceInput>
        {
            new() { LeaveType = "Semester", Balance = 18m, Unit = "days" }
        }
    };

    [Fact]
    public async Task Create_computes_summary_and_reconciles_to_net()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var created = await (await client.PostAsJsonAsync("/api/v1/payslips",
            SamplePayslip(new DateOnly(2026, 6, 25)))).Content.ReadFromJsonAsync<PayslipDto>();

        created!.Summary.Gross.Should().Be(55_000m);
        created.Summary.Benefits.Should().Be(2_000m);
        created.Summary.Tax.Should().Be(13_443m);
        created.Summary.Net.Should().Be(41_557m);
        created.Summary.GrossLabel.Should().Be("Bruttolön");
        created.Reconciliation.IsReconciled.Should().BeTrue();
        created.Reconciliation.Difference.Should().Be(0m);
        created.Status.Should().Be("Draft");
        created.LeaveBalances.Should().ContainSingle().Which.LeaveType.Should().Be("Semester");
    }

    [Fact]
    public async Task Personnummer_is_masked_and_never_returned_in_clear()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/payslips", SamplePayslip(new DateOnly(2026, 6, 25)));
        var raw = await response.Content.ReadAsStringAsync();

        // The clear personnummer must not appear anywhere in the payload.
        raw.Should().NotContain("19900101-1234");
        raw.Should().NotContain("1234");

        var created = await client.GetFromJsonAsync<PayslipDto>(
            $"/api/v1/payslips/{(await response.Content.ReadFromJsonAsync<PayslipDto>())!.Id}");
        created!.PersonnummerMasked.Should().Be("19900101-****");
    }

    [Fact]
    public async Task YearToDate_aggregates_line_items_across_the_year()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        await client.PostAsJsonAsync("/api/v1/payslips", SamplePayslip(new DateOnly(2026, 5, 25), personnummer: null));
        var second = await (await client.PostAsJsonAsync("/api/v1/payslips",
            SamplePayslip(new DateOnly(2026, 6, 25), personnummer: null))).Content.ReadFromJsonAsync<PayslipDto>();

        var detail = await client.GetFromJsonAsync<PayslipDto>($"/api/v1/payslips/{second!.Id}");

        // One month gross is 55 000; two payslips in 2026 → YTD gross 110 000, YTD net 83 114.
        detail!.Summary.Gross.Should().Be(55_000m);
        detail.YearToDateSummary.Gross.Should().Be(110_000m);
        detail.YearToDateSummary.Net.Should().Be(83_114m);
    }

    [Fact]
    public async Task Post_creates_income_transaction_and_marks_posted()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client);
        var category = await CreateCategoryAsync(client, "Salary");
        var payslip = await (await client.PostAsJsonAsync("/api/v1/payslips",
            SamplePayslip(new DateOnly(2026, 6, 25)))).Content.ReadFromJsonAsync<PayslipDto>();

        var postResponse = await client.PostAsJsonAsync($"/api/v1/payslips/{payslip!.Id}/post",
            new PostPayslipRequest { AccountId = account.Id, CategoryId = category.Id });
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await postResponse.Content.ReadFromJsonAsync<PostPayslipResultDto>();
        result!.Amount.Should().Be(41_557m);
        result.Status.Should().Be("Posted");

        // The income transaction exists on the account.
        var txns = await client.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions");
        txns!.TotalCount.Should().Be(1);
        txns.Items[0].Type.Should().Be("Income");
        txns.Items[0].Amount.Should().Be(41_557m);

        // The payslip is now posted and read-only.
        var reloaded = await client.GetFromJsonAsync<PayslipDto>($"/api/v1/payslips/{payslip.Id}");
        reloaded!.Status.Should().Be("Posted");
        reloaded.PostedTransactionId.Should().NotBeNull();
    }

    [Fact]
    public async Task Posting_twice_is_rejected()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client);
        var category = await CreateCategoryAsync(client, "Salary");
        var payslip = await (await client.PostAsJsonAsync("/api/v1/payslips",
            SamplePayslip(new DateOnly(2026, 6, 25)))).Content.ReadFromJsonAsync<PayslipDto>();
        var post = new PostPayslipRequest { AccountId = account.Id, CategoryId = category.Id };

        (await client.PostAsJsonAsync($"/api/v1/payslips/{payslip!.Id}/post", post))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsJsonAsync($"/api/v1/payslips/{payslip.Id}/post", post))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Editing_a_posted_payslip_is_rejected()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var account = await CreateAccountAsync(client);
        var category = await CreateCategoryAsync(client, "Salary");
        var payslip = await (await client.PostAsJsonAsync("/api/v1/payslips",
            SamplePayslip(new DateOnly(2026, 6, 25)))).Content.ReadFromJsonAsync<PayslipDto>();
        await client.PostAsJsonAsync($"/api/v1/payslips/{payslip!.Id}/post",
            new PostPayslipRequest { AccountId = account.Id, CategoryId = category.Id });

        var update = SamplePayslip(new DateOnly(2026, 6, 25));
        update.EmployerName = "Changed AB";
        (await client.PutAsJsonAsync($"/api/v1/payslips/{payslip.Id}", update))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_replaces_line_items_and_re_reconciles()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var payslip = await (await client.PostAsJsonAsync("/api/v1/payslips",
            SamplePayslip(new DateOnly(2026, 6, 25)))).Content.ReadFromJsonAsync<PayslipDto>();

        var update = SamplePayslip(new DateOnly(2026, 6, 25), personnummer: null);
        update.DeclaredNet = 30_000m;
        update.LineItems = new List<PayslipLineItemInput>
        {
            new() { Type = "Earning", Label = "Grundlön", Amount = 40_000m },
            new() { Type = "Tax", Label = "Skatt", Amount = 10_000m }
        };

        var updated = await (await client.PutAsJsonAsync($"/api/v1/payslips/{payslip!.Id}", update))
            .Content.ReadFromJsonAsync<PayslipDto>();

        updated!.LineItems.Should().HaveCount(2);
        updated.Summary.Net.Should().Be(30_000m);
        updated.Reconciliation.IsReconciled.Should().BeTrue();
        // Personnummer left unchanged when the update omits it.
        updated.PersonnummerMasked.Should().Be("19900101-****");
    }

    [Fact]
    public async Task Unsupported_country_is_rejected()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var request = SamplePayslip(new DateOnly(2026, 6, 25), personnummer: null);
        request.Country = "Norway";

        (await client.PostAsJsonAsync("/api/v1/payslips", request))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

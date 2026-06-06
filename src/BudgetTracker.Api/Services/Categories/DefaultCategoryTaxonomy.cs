namespace BudgetTracker.Api.Services.Categories;

/// <summary>
/// A node in the seed taxonomy spec: a name plus optional children. Mirrors the headings from the
/// user's Excel tracker (TASK 3.6).
/// </summary>
public sealed record SeedCategory(string Name, params SeedCategory[] Children);

/// <summary>
/// The default Excel/Swedish category tree a new household (or first-time user) starts with.
/// </summary>
public static class DefaultCategoryTaxonomy
{
    /// <summary>
    /// Root categories with their sub-items, in display order.
    /// </summary>
    public static readonly IReadOnlyList<SeedCategory> Roots = new SeedCategory[]
    {
        new("Housing",
            new("Rent"),
            new("Home Insurance"),
            new("Broadband / Internet")),

        new("Household & Personal Spending",
            new("Groceries and Essentials",
                new("Groceries"),
                new("Household Items"),
                new("Medical / Pharmacy"),
                new("Misc Essentials")),
            new("Lifestyle & Shopping",
                new("Restaurants"),
                new("Electronics"),
                new("Clothing"),
                new("Plants"),
                new("Other Purchases"))),

        new("Health & Fitness",
            new("Gym (You)"),
            new("Gym (Wife)")),

        new("Subscriptions",
            new("HP"),
            new("ChatGPT"),
            new("Claude"),
            new("iPhone Payments"),
            new("Tre"),
            new("Hallon"),
            new("PC services"),
            new("Misc Subscriptions")),

        new("Work-Related",
            new("Unionen"),
            new("Employment Insurance"),
            new("Work Meals / Restaurants")),

        new("Transport & Car",
            new("Car Costs"),
            new("Parking"),
            new("Paid Parking"),
            new("Car Wash"),
            new("Toll"),
            new("Car EMI / Loan"),
            new("Fuel"),
            new("Car Insurance"),
            new("Accessories"),
            new("Maintenance"),
            new("Misc Car Costs"),
            new("Public Transport"),
            new("Västtrafik"),
            new("Road Tax / Car Tax")),

        new("Travel & Trips",
            new("Tickets / Flights"),
            new("Hotel"),
            new("Bus / Local Transport"),
            new("Food"),
            new("Attractions"),
            new("Misc Trip Expenses")),

        new("India-Related Finances",
            new("Payments in INR"),
            new("ICICI Credit Card"),
            new("Home Loan"),
            new("Netflix"),
            new("Misc INR Purchases"),
            new("Transfers",
                new("INR Sent"),
                new("Transfer Fees"),
                new("Conversions"),
                new("SEK Equivalent"))),

        new("Financial Summary",
            new("Savings"),
            new("Investments"),
            new("Total Income"),
            new("Total Expenses"),
            new("Remaining Balance"))
    };
}

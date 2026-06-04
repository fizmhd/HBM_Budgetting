using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BudgetTracker.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Respawn;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using NSubstitute;

namespace BudgetTracker.Api.IntegrationTests.Infrastructure;

public class DatabaseFixture : IAsyncLifetime
{
    public CustomWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new CustomWebApplicationFactory();
        await Factory.InitializeAsync();
        await SeedDataAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await Factory.ResetDatabaseAsync();
        await SeedDataAsync(); // Re-seed after clean
    }
    
    public async Task SeedDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Users.AnyAsync())
        {
            var users = new[]
            {
                new User { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Email = "confirmed@test.com", IsProfileComplete = true },
                new User { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Email = "unconfirmed@test.com", IsProfileComplete = false },
                new User { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Email = "incomplete@test.com", IsProfileComplete = false },
                new User { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Email = "locked@test.com", IsProfileComplete = true, FailedLoginAttempts = 5, LockoutEndUtc = DateTime.UtcNow.AddMinutes(15) }
            };

            await db.Users.AddRangeAsync(users);
            await db.SaveChangesAsync();
        }
    }
}

using AutoFixture;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.UnitTests.Helpers;

public static class TestDataBuilder
{
    private static readonly Fixture _fixture = new();

    static TestDataBuilder()
    {
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    public static User CreateUser(bool isProfileComplete = true)
    {
        var user = _fixture.Build<User>()
            .Without(u => u.RefreshTokens)
            .Without(u => u.ExternalLogins)
            .Create();
            
        user.IsProfileComplete = isProfileComplete;
        user.LockoutEndUtc = null;
        user.FailedLoginAttempts = 0;
        
        return user;
    }
}

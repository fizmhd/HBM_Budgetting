using NSubstitute;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;

namespace BudgetTracker.Api.UnitTests.Helpers;

public static class MockRepositoryFactory
{
    public static IUserRepository CreateUserRepository()
    {
        return Substitute.For<IUserRepository>();
    }
}

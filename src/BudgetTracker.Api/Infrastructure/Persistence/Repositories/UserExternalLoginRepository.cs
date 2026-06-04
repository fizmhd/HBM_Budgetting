using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for UserExternalLogin-specific operations
/// </summary>
public class UserExternalLoginRepository : Repository<UserExternalLogin>, IUserExternalLoginRepository
{
    public UserExternalLoginRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<UserExternalLogin?> GetByProviderAsync(
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                el => el.Provider == provider && el.ProviderKey == providerKey,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<UserExternalLogin>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(el => el.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}

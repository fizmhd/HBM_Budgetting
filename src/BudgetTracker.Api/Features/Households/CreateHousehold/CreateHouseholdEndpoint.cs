using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Households;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Households.CreateHousehold;

/// <summary>
/// Creates a household; the caller becomes its Owner member. The household also starts with the
/// default category taxonomy (TASK 3.6), unless the caller already has categories to share.
/// </summary>
public class CreateHouseholdEndpoint : Endpoint<CreateHouseholdRequest, HouseholdDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdMemberRepository _members;
    private readonly ICategoryRepository _categories;
    private readonly ICategorySeeder _seeder;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public CreateHouseholdEndpoint(
        ICurrentUserService currentUser,
        IHouseholdRepository households,
        IHouseholdMemberRepository members,
        ICategoryRepository categories,
        ICategorySeeder seeder,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _households = households;
        _members = members;
        _categories = categories;
        _seeder = seeder;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/households");
        Validator<CreateHouseholdRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 30, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CreateHouseholdRequest req, CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        // MVP: a user belongs to at most one household.
        var existing = await _members.GetByUserIdAsync(user.Id, ct);
        if (existing is not null)
        {
            ThrowError("You already belong to a household.", 409);
            return;
        }

        var now = DateTime.UtcNow;
        var household = new Household
        {
            Name = req.Name.Trim(),
            CreatedByUserId = user.Id
        };
        household.Members.Add(new HouseholdMember
        {
            UserId = user.Id,
            DisplayName = HouseholdMapping.DisplayNameFor(user),
            Role = HouseholdRole.Owner,
            JoinedAt = now
        });

        await _households.AddAsync(household, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Seed the default taxonomy for the new household, unless the user already has categories
        // (avoids duplicating a tree they imported individually before forming a household).
        if (!await _categories.HasAnyAsync(user.Id, household.Id, ct))
        {
            foreach (var category in _seeder.BuildDefaults(user.Id, household.Id))
            {
                await _categories.AddAsync(category, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
        }

        await SendOkAsync(household.ToDto(household.Members), ct);
    }
}

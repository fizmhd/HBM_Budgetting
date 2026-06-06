using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using FluentAssertions;

namespace BudgetTracker.Api.UnitTests.Persistence;

/// <summary>
/// Unit tests for the owner/visibility privacy rule (TASK 1.1).
/// </summary>
public class VisibilityFilterTests
{
    // A minimal concrete owned entity so the generic helper can be exercised in isolation.
    private sealed class TestRecord : OwnedEntity
    {
    }

    private static readonly Guid Me = Guid.NewGuid();
    private static readonly Guid Spouse = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();
    private static readonly Guid MyHousehold = Guid.NewGuid();
    private static readonly Guid OtherHousehold = Guid.NewGuid();

    private static TestRecord Record(Guid owner, Visibility visibility, Guid? householdId) =>
        new() { Id = Guid.NewGuid(), OwnerUserId = owner, Visibility = visibility, HouseholdId = householdId };

    [Fact]
    public void Owner_sees_their_own_individual_record()
    {
        var record = Record(Me, Visibility.Individual, null);
        var visible = new[] { record }.AsQueryable().VisibleTo(Me, MyHousehold).ToList();
        visible.Should().ContainSingle().Which.Should().Be(record);
    }

    [Fact]
    public void Household_member_sees_a_household_shared_record()
    {
        var shared = Record(Spouse, Visibility.HouseholdShared, MyHousehold);
        var visible = new[] { shared }.AsQueryable().VisibleTo(Me, MyHousehold).ToList();
        visible.Should().ContainSingle().Which.Should().Be(shared);
    }

    [Fact]
    public void Non_member_does_not_see_a_household_shared_record_of_another_household()
    {
        var shared = Record(Stranger, Visibility.HouseholdShared, OtherHousehold);
        var visible = new[] { shared }.AsQueryable().VisibleTo(Me, MyHousehold).ToList();
        visible.Should().BeEmpty();
    }

    [Fact]
    public void Another_users_individual_record_is_hidden()
    {
        var theirs = Record(Spouse, Visibility.Individual, MyHousehold);
        var visible = new[] { theirs }.AsQueryable().VisibleTo(Me, MyHousehold).ToList();
        visible.Should().BeEmpty();
    }

    [Fact]
    public void User_without_a_household_only_sees_their_own_records()
    {
        var mine = Record(Me, Visibility.Individual, null);
        var shared = Record(Spouse, Visibility.HouseholdShared, MyHousehold);
        var visible = new[] { mine, shared }.AsQueryable().VisibleTo(Me, householdId: null).ToList();
        visible.Should().ContainSingle().Which.Should().Be(mine);
    }

    [Fact]
    public void Mixed_set_returns_only_owned_and_household_shared()
    {
        var records = new[]
        {
            Record(Me, Visibility.Individual, null),           // mine, private -> visible
            Record(Me, Visibility.HouseholdShared, MyHousehold), // mine, shared -> visible
            Record(Spouse, Visibility.HouseholdShared, MyHousehold), // spouse shared in my household -> visible
            Record(Spouse, Visibility.Individual, MyHousehold), // spouse private -> hidden
            Record(Stranger, Visibility.HouseholdShared, OtherHousehold), // other household -> hidden
        };

        var visible = records.AsQueryable().VisibleTo(Me, MyHousehold).ToList();

        visible.Should().HaveCount(3);
        visible.Should().OnlyContain(r =>
            (r.OwnerUserId == Me) ||
            (r.Visibility == Visibility.HouseholdShared && r.HouseholdId == MyHousehold));
    }

    [Fact]
    public void IsVisibleTo_matches_the_query_helper_semantics()
    {
        Record(Me, Visibility.Individual, null).IsVisibleTo(Me, MyHousehold).Should().BeTrue();
        Record(Spouse, Visibility.HouseholdShared, MyHousehold).IsVisibleTo(Me, MyHousehold).Should().BeTrue();
        Record(Spouse, Visibility.Individual, MyHousehold).IsVisibleTo(Me, MyHousehold).Should().BeFalse();
        Record(Stranger, Visibility.HouseholdShared, OtherHousehold).IsVisibleTo(Me, MyHousehold).Should().BeFalse();
    }
}

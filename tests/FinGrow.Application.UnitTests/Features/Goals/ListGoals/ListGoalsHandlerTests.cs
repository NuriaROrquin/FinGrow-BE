namespace FinGrow.Application.UnitTests.Features.Goals.ListGoals;

using Common;
using DTOs;
using FinGrow.Application.Features.Goals.ListGoals;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class ListGoalsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeGoalRepository _goals = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    private Task<Result<IReadOnlyList<GoalResponse>>> ListAsync() =>
        new ListGoalsHandler(_goals, _clock).Handle(new ListGoalsQuery(_employeeId), CancellationToken.None);

    private Goal StoreGoal(decimal target = 1000m, DateOnly? deadline = null)
    {
        var goal = Goal.Create(_employeeId, "Vacaciones", Money.From(target, Currency.ARS), deadline ?? Today.AddDays(30), Now);
        _goals.Add(goal);
        return goal;
    }

    [Fact]
    public async Task Only_the_goals_of_the_employee_are_listed_with_their_accumulated_amount()
    {
        var own = StoreGoal();
        own.AddContribution(Money.From(400m, Currency.ARS), Today, null, Now);
        _goals.Add(Goal.Create(Guid.CreateVersion7(), "Auto", Money.From(1000m, Currency.ARS), Today.AddMonths(6), Now));

        var result = await ListAsync();

        var goal = result.Value.ShouldHaveSingleItem();
        goal.Name.ShouldBe("Vacaciones");
        goal.CurrentAmount.ShouldBe(400m);
    }

    [Fact]
    public async Task Each_goal_shows_its_percentage_remaining_amount_and_days_left()
    {
        var own = StoreGoal(target: 1000m, deadline: Today.AddDays(30));
        own.AddContribution(Money.From(250m, Currency.ARS), Today, null, Now);

        var goal = (await ListAsync()).Value.ShouldHaveSingleItem();

        goal.ProgressPercentage.ShouldBe(25m);
        goal.RemainingAmount.ShouldBe(750m);
        goal.DaysRemaining.ShouldBe(30);
        goal.IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public async Task A_goal_past_its_deadline_is_overdue_and_never_shows_negative_days()
    {
        StoreGoal(deadline: Today.AddDays(5));
        _clock.UtcNow = Now.AddDays(12);

        var goal = (await ListAsync()).Value.ShouldHaveSingleItem();

        goal.IsOverdue.ShouldBeTrue();
        goal.DaysRemaining.ShouldBe(0);
    }

    [Fact]
    public async Task A_goal_is_not_overdue_on_its_deadline_day()
    {
        StoreGoal(deadline: Today.AddDays(5));
        _clock.UtcNow = Now.AddDays(5);

        var goal = (await ListAsync()).Value.ShouldHaveSingleItem();

        goal.IsOverdue.ShouldBeFalse();
        goal.DaysRemaining.ShouldBe(0);
    }

    [Fact]
    public async Task Late_at_night_in_argentina_the_deadline_day_is_not_yet_overdue()
    {
        StoreGoal(deadline: Today.AddDays(5));
        // 23:30 en Buenos Aires del dia limite; en UTC ya es el dia siguiente.
        _clock.UtcNow = new DateTimeOffset(Today.AddDays(6), new TimeOnly(2, 30), TimeSpan.Zero);

        var goal = (await ListAsync()).Value.ShouldHaveSingleItem();

        goal.IsOverdue.ShouldBeFalse();
        goal.DaysRemaining.ShouldBe(0);
    }

    [Fact]
    public async Task An_achieved_goal_past_its_deadline_is_not_overdue()
    {
        var own = StoreGoal(target: 1000m, deadline: Today.AddDays(5));
        own.AddContribution(Money.From(1000m, Currency.ARS), Today, null, Now);
        _clock.UtcNow = Now.AddDays(12);

        var goal = (await ListAsync()).Value.ShouldHaveSingleItem();

        goal.Status.ShouldBe(GoalStatus.Achieved);
        goal.IsOverdue.ShouldBeFalse();
        goal.RemainingAmount.ShouldBe(0m);
    }
}

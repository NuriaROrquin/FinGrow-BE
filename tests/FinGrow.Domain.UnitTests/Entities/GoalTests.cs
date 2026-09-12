namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class GoalTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Deadline = new(2026, 12, 31);

    [Fact]
    public void A_goal_is_created_active_and_at_zero()
    {
        var goal = CreateGoal();

        goal.Status.ShouldBe(GoalStatus.Active);
        goal.CurrentAmount.ShouldBe(Money.Zero(Currency.ARS));
        goal.ProgressPercentage.ShouldBe(0m);
    }

    [Fact]
    public void Progress_accumulates_the_contributions()
    {
        var goal = CreateGoal();

        goal.AddProgress(Money.From(250000m, Currency.ARS), Now);
        goal.AddProgress(Money.From(250000m, Currency.ARS), Now.AddDays(30));

        goal.CurrentAmount.Amount.ShouldBe(500000m);
        goal.ProgressPercentage.ShouldBe(50m);
        goal.RemainingAmount.Amount.ShouldBe(500000m);
    }

    [Fact]
    public void The_goal_is_marked_achieved_when_it_reaches_the_target()
    {
        var goal = CreateGoal();
        var achievedAt = Now.AddDays(60);

        goal.AddProgress(Money.From(1000000m, Currency.ARS), achievedAt);

        goal.Status.ShouldBe(GoalStatus.Achieved);
        goal.AchievedAt.ShouldBe(achievedAt);
    }

    [Fact]
    public void Exceeding_the_target_does_not_push_progress_above_a_hundred()
    {
        var goal = CreateGoal();

        goal.AddProgress(Money.From(1500000m, Currency.ARS), Now);

        goal.ProgressPercentage.ShouldBe(100m);
        goal.RemainingAmount.IsZero.ShouldBeTrue();
    }

    [Fact]
    public void An_already_achieved_goal_does_not_accept_more_progress()
    {
        var goal = CreateGoal();
        goal.AddProgress(Money.From(1000000m, Currency.ARS), Now);

        Should.Throw<DomainException>(() => goal.AddProgress(Money.From(1m, Currency.ARS), Now));
    }

    [Fact]
    public void An_already_achieved_goal_cannot_be_cancelled()
    {
        var goal = CreateGoal();
        goal.AddProgress(Money.From(1000000m, Currency.ARS), Now);

        Should.Throw<DomainException>(() => goal.Cancel(Now));
    }

    [Fact]
    public void A_goal_with_a_past_deadline_is_not_created()
    {
        Should.Throw<DomainException>(() => Goal.Create(
            EmployeeId,
            "Viaje",
            Money.From(1000000m, Currency.ARS),
            new DateOnly(2026, 1, 1),
            Now));
    }

    [Fact]
    public void Remaining_days_are_counted_against_the_deadline()
    {
        var goal = CreateGoal();

        goal.DaysRemaining(new DateOnly(2026, 12, 21)).ShouldBe(10);
        goal.DaysRemaining(new DateOnly(2027, 1, 10)).ShouldBe(-10);
    }

    private static Goal CreateGoal() => Goal.Create(
        EmployeeId,
        "Fondo de emergencia",
        Money.From(1000000m, Currency.ARS),
        Deadline,
        Now);
}

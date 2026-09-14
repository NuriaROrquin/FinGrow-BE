namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class GoalTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 3, 15);
    private static readonly DateOnly Deadline = new(2026, 12, 31);

    [Fact]
    public void A_goal_is_created_active_at_zero_and_without_contributions()
    {
        var goal = CreateGoal();

        goal.Status.ShouldBe(GoalStatus.Active);
        goal.CurrentAmount.ShouldBe(Money.Zero(Currency.ARS));
        goal.ProgressPercentage.ShouldBe(0m);
        goal.Contributions.ShouldBeEmpty();
    }

    [Fact]
    public void The_accumulated_amount_is_the_sum_of_the_contributions()
    {
        var goal = CreateGoal();

        goal.AddContribution(Money.From(250000m, Currency.ARS), Today, "Aguinaldo", Now);
        goal.AddContribution(Money.From(250000m, Currency.ARS), Today.AddDays(30), null, Now.AddDays(30));

        goal.Contributions.Count.ShouldBe(2);
        goal.CurrentAmount.Amount.ShouldBe(500000m);
        goal.ProgressPercentage.ShouldBe(50m);
        goal.RemainingAmount.Amount.ShouldBe(500000m);
    }

    [Fact]
    public void Removing_a_contribution_recalculates_the_accumulated_amount()
    {
        var goal = CreateGoal();
        var mistaken = goal.AddContribution(Money.From(300000m, Currency.ARS), Today, null, Now);
        goal.AddContribution(Money.From(200000m, Currency.ARS), Today, null, Now);

        goal.RemoveContribution(mistaken.Id, Now.AddHours(1));

        goal.Contributions.Count.ShouldBe(1);
        goal.CurrentAmount.Amount.ShouldBe(200000m);
    }

    [Fact]
    public void Removing_a_contribution_that_does_not_belong_to_the_goal_fails()
    {
        var goal = CreateGoal();

        Should.Throw<DomainException>(() => goal.RemoveContribution(Guid.CreateVersion7(), Now));
    }

    [Fact]
    public void A_contribution_in_another_currency_is_rejected()
    {
        var goal = CreateGoal();

        Should.Throw<DomainException>(() =>
            goal.AddContribution(Money.From(100m, Currency.USD), Today, null, Now));
    }

    [Fact]
    public void A_zero_contribution_is_rejected()
    {
        var goal = CreateGoal();

        Should.Throw<DomainException>(() =>
            goal.AddContribution(Money.Zero(Currency.ARS), Today, null, Now));
    }

    [Fact]
    public void The_goal_is_marked_achieved_when_it_reaches_the_target()
    {
        var goal = CreateGoal();
        var achievedAt = Now.AddDays(60);

        goal.AddContribution(Money.From(1000000m, Currency.ARS), Today.AddDays(60), null, achievedAt);

        goal.Status.ShouldBe(GoalStatus.Achieved);
        goal.AchievedAt.ShouldBe(achievedAt);
    }

    [Fact]
    public void Removing_the_contribution_that_achieved_the_goal_makes_it_active_again()
    {
        var goal = CreateGoal();
        var decisive = goal.AddContribution(Money.From(1000000m, Currency.ARS), Today, null, Now);

        goal.RemoveContribution(decisive.Id, Now.AddHours(1));

        goal.Status.ShouldBe(GoalStatus.Active);
        goal.AchievedAt.ShouldBeNull();
        goal.CurrentAmount.IsZero.ShouldBeTrue();
    }

    [Fact]
    public void Exceeding_the_target_does_not_push_progress_above_a_hundred()
    {
        var goal = CreateGoal();

        goal.AddContribution(Money.From(1500000m, Currency.ARS), Today, null, Now);

        goal.ProgressPercentage.ShouldBe(100m);
        goal.RemainingAmount.IsZero.ShouldBeTrue();
    }

    [Fact]
    public void An_already_achieved_goal_does_not_accept_more_contributions()
    {
        var goal = CreateGoal();
        goal.AddContribution(Money.From(1000000m, Currency.ARS), Today, null, Now);

        Should.Throw<DomainException>(() =>
            goal.AddContribution(Money.From(1m, Currency.ARS), Today, null, Now));
    }

    [Fact]
    public void An_already_achieved_goal_cannot_be_cancelled()
    {
        var goal = CreateGoal();
        goal.AddContribution(Money.From(1000000m, Currency.ARS), Today, null, Now);

        Should.Throw<DomainException>(() => goal.Cancel(Now));
    }

    [Fact]
    public void A_cancelled_goal_does_not_let_contributions_be_removed()
    {
        var goal = CreateGoal();
        var contribution = goal.AddContribution(Money.From(1000m, Currency.ARS), Today, null, Now);
        goal.Cancel(Now);

        Should.Throw<DomainException>(() => goal.RemoveContribution(contribution.Id, Now));
    }

    [Fact]
    public void Raising_the_target_above_the_accumulated_amount_reopens_an_achieved_goal()
    {
        var goal = CreateGoal();
        goal.AddContribution(Money.From(1000000m, Currency.ARS), Today, null, Now);

        goal.UpdateDetails("Fondo de emergencia", Money.From(2000000m, Currency.ARS), Deadline, Now.AddDays(1));

        goal.Status.ShouldBe(GoalStatus.Active);
        goal.AchievedAt.ShouldBeNull();
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

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

    [Fact]
    public void A_deadline_of_today_in_argentina_is_accepted_late_at_night()
    {
        var lateNightInBuenosAires = new DateTimeOffset(2026, 9, 24, 2, 30, 0, TimeSpan.Zero);

        var goal = Goal.Create(EmployeeId, "Regalo", Money.From(1000m, Currency.ARS), new DateOnly(2026, 9, 23), lateNightInBuenosAires);

        goal.Deadline.ShouldBe(new DateOnly(2026, 9, 23));
    }

    [Fact]
    public void The_monthly_commitment_splits_the_target_between_the_creation_and_deadline_months()
    {
        // Creada el 15 de marzo con limite el 31 de diciembre: de marzo a diciembre son 10 meses.
        var goal = CreateGoal();

        goal.PlannedMonths.ShouldBe(10);
        goal.MonthlyCommitment.ShouldBe(Money.From(100000m, Currency.ARS));
    }

    [Fact]
    public void A_goal_due_in_the_same_month_it_was_created_commits_everything_that_month()
    {
        var goal = Goal.Create(EmployeeId, "Regalo", Money.From(5000m, Currency.ARS), Today.AddDays(5), Now);

        goal.PlannedMonths.ShouldBe(1);
        goal.MonthlyCommitment.ShouldBe(Money.From(5000m, Currency.ARS));
    }

    [Fact]
    public void A_goal_commits_savings_only_between_its_creation_and_deadline_months()
    {
        var goal = CreateGoal();

        goal.IsCommittedIn(new DateOnly(2026, 2, 28)).ShouldBeFalse();
        goal.IsCommittedIn(new DateOnly(2026, 3, 1)).ShouldBeTrue();
        goal.IsCommittedIn(new DateOnly(2026, 12, 15)).ShouldBeTrue();
        goal.IsCommittedIn(new DateOnly(2027, 1, 1)).ShouldBeFalse();
    }

    [Fact]
    public void An_achieved_goal_still_commits_in_the_month_it_was_reached_but_not_after()
    {
        var goal = CreateGoal();
        goal.AddContribution(Money.From(1000000m, Currency.ARS), Today, null, Now);

        goal.IsCommittedIn(new DateOnly(2026, 3, 20)).ShouldBeTrue();
        goal.IsCommittedIn(new DateOnly(2026, 4, 1)).ShouldBeFalse();
    }

    [Fact]
    public void A_goal_completed_with_a_backdated_contribution_stops_committing_after_the_contribution_month()
    {
        var goal = CreateGoal();
        goal.AddContribution(Money.From(400000m, Currency.ARS), new DateOnly(2026, 5, 10), null, new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero));
        goal.AddContribution(Money.From(600000m, Currency.ARS), new DateOnly(2026, 6, 5), null, new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));

        goal.AchievedAt.ShouldBe(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
        goal.ReachedOn.ShouldBe(new DateOnly(2026, 6, 5));
        goal.IsCommittedIn(new DateOnly(2026, 6, 1)).ShouldBeTrue();
        goal.IsCommittedIn(new DateOnly(2026, 7, 1)).ShouldBeFalse();
    }

    [Fact]
    public void The_target_is_reached_by_contribution_date_not_by_registration_order()
    {
        var goal = CreateGoal();
        goal.AddContribution(Money.From(700000m, Currency.ARS), new DateOnly(2026, 6, 1), null, Now);
        goal.AddContribution(Money.From(300000m, Currency.ARS), new DateOnly(2026, 4, 1), null, Now);

        goal.ReachedOn.ShouldBe(new DateOnly(2026, 6, 1));
    }

    [Fact]
    public void A_goal_in_progress_has_not_been_reached()
    {
        var goal = CreateGoal();
        goal.AddContribution(Money.From(1000m, Currency.ARS), Today, null, Now);

        goal.ReachedOn.ShouldBeNull();
    }

    [Fact]
    public void A_cancelled_goal_commits_nothing()
    {
        var goal = CreateGoal();
        goal.Cancel(Now);

        goal.IsCommittedIn(Today).ShouldBeFalse();
    }

    [Fact]
    public void An_active_goal_is_overdue_only_after_its_deadline()
    {
        var goal = CreateGoal();

        goal.IsOverdue(Deadline).ShouldBeFalse();
        goal.IsOverdue(Deadline.AddDays(1)).ShouldBeTrue();
    }

    [Fact]
    public void An_achieved_goal_is_never_overdue()
    {
        var goal = CreateGoal();
        goal.AddContribution(Money.From(1000000m, Currency.ARS), Today, null, Now);

        goal.IsOverdue(Deadline.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void A_cancelled_goal_is_never_overdue()
    {
        var goal = CreateGoal();
        goal.Cancel(Now);

        goal.IsOverdue(Deadline.AddDays(1)).ShouldBeFalse();
    }

    private static Goal CreateGoal() => Goal.Create(
        EmployeeId,
        "Fondo de emergencia",
        Money.From(1000000m, Currency.ARS),
        Deadline,
        Now);
}

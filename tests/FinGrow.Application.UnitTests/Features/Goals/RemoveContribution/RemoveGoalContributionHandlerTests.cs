namespace FinGrow.Application.UnitTests.Features.Goals.RemoveContribution;

using Common;
using FinGrow.Application.Features.Goals.RemoveContribution;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class RemoveGoalContributionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeGoalRepository _goals = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly RemoveGoalContributionHandler _handler;

    public RemoveGoalContributionHandlerTests() =>
        _handler = new RemoveGoalContributionHandler(_goals, _unitOfWork, new FakeDateTimeProvider(Now));

    private Goal StoreGoal(Guid? employeeId = null)
    {
        var goal = Goal.Create(employeeId ?? _employeeId, "Vacaciones", Money.From(1000m, Currency.ARS), Today.AddMonths(6), Now);
        _goals.Add(goal);
        return goal;
    }

    [Fact]
    public async Task Removing_a_contribution_recalculates_the_accumulated_amount()
    {
        var goal = StoreGoal();
        goal.AddContribution(Money.From(300m, Currency.ARS), Today, null, Now);
        var mistake = goal.AddContribution(Money.From(200m, Currency.ARS), Today, null, Now);

        var result = await _handler.Handle(
            new RemoveGoalContributionCommand(_employeeId, goal.Id, mistake.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrentAmount.ShouldBe(300m);
        result.Value.ProgressPercentage.ShouldBe(30m);
        goal.Contributions.ShouldNotContain(contribution => contribution.Id == mistake.Id);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Removing_the_contribution_that_reached_the_target_reactivates_the_goal()
    {
        var goal = StoreGoal();
        var contribution = goal.AddContribution(Money.From(1000m, Currency.ARS), Today, null, Now);

        var result = await _handler.Handle(
            new RemoveGoalContributionCommand(_employeeId, goal.Id, contribution.Id),
            CancellationToken.None);

        result.Value.Status.ShouldBe(GoalStatus.Active);
        result.Value.CurrentAmount.ShouldBe(0m);
    }

    [Fact]
    public async Task An_unknown_contribution_is_reported_as_not_found()
    {
        var goal = StoreGoal();

        var result = await _handler.Handle(
            new RemoveGoalContributionCommand(_employeeId, goal.Id, Guid.CreateVersion7()),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_goal_of_another_employee_is_reported_as_not_found()
    {
        var goal = StoreGoal(employeeId: Guid.CreateVersion7());
        var contribution = goal.AddContribution(Money.From(100m, Currency.ARS), Today, null, Now);

        var result = await _handler.Handle(
            new RemoveGoalContributionCommand(_employeeId, goal.Id, contribution.Id),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        goal.Contributions.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_cancelled_goal_cannot_lose_contributions()
    {
        var goal = StoreGoal();
        var contribution = goal.AddContribution(Money.From(100m, Currency.ARS), Today, null, Now);
        goal.Cancel(Now);

        var result = await _handler.Handle(
            new RemoveGoalContributionCommand(_employeeId, goal.Id, contribution.Id),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        goal.Contributions.ShouldHaveSingleItem();
    }
}

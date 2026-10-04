namespace FinGrow.Application.UnitTests.Features.Goals.DeleteGoal;

using Common;
using FinGrow.Application.Features.Goals.DeleteGoal;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class DeleteGoalHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeGoalRepository _goals = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DeleteGoalHandler _handler;

    public DeleteGoalHandlerTests() =>
        _handler = new DeleteGoalHandler(_goals, _unitOfWork, new FakeDateTimeProvider(Now));

    private Goal StoreGoal(Guid? employeeId = null)
    {
        var goal = Goal.Create(employeeId ?? _employeeId, "Vacaciones", Money.From(1000m, Currency.ARS), Today.AddMonths(6), Now);
        _goals.Add(goal);
        return goal;
    }

    [Fact]
    public async Task Deleting_a_goal_cancels_it_and_keeps_its_contributions()
    {
        var goal = StoreGoal();
        goal.AddContribution(Money.From(300m, Currency.ARS), Today, null, Now);

        var result = await _handler.Handle(new DeleteGoalCommand(_employeeId, goal.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        goal.Status.ShouldBe(GoalStatus.Cancelled);
        goal.Contributions.ShouldHaveSingleItem();
        _goals.Goals.ShouldContain(goal);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_goal_of_another_employee_is_reported_as_not_found()
    {
        var goal = StoreGoal(employeeId: Guid.CreateVersion7());

        var result = await _handler.Handle(new DeleteGoalCommand(_employeeId, goal.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        goal.Status.ShouldBe(GoalStatus.Active);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_already_deleted_goal_is_reported_as_not_found()
    {
        var goal = StoreGoal();
        goal.Cancel(Now);

        var result = await _handler.Handle(new DeleteGoalCommand(_employeeId, goal.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_unknown_goal_is_reported_as_not_found()
    {
        var result = await _handler.Handle(
            new DeleteGoalCommand(_employeeId, Guid.CreateVersion7()),
            CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task An_achieved_goal_can_be_deleted()
    {
        var goal = StoreGoal();
        goal.AddContribution(Money.From(1000m, Currency.ARS), Today, null, Now);

        var result = await _handler.Handle(new DeleteGoalCommand(_employeeId, goal.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        goal.Status.ShouldBe(GoalStatus.Cancelled);
        goal.Contributions.ShouldHaveSingleItem();
        _unitOfWork.SaveCount.ShouldBe(1);
    }
}

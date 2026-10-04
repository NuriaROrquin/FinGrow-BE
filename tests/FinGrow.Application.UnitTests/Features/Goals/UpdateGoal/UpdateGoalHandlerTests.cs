namespace FinGrow.Application.UnitTests.Features.Goals.UpdateGoal;

using Common;
using FinGrow.Application.Features.Goals.UpdateGoal;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class UpdateGoalHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeGoalRepository _goals = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly UpdateGoalHandler _handler;

    public UpdateGoalHandlerTests() =>
        _handler = new UpdateGoalHandler(_goals, _unitOfWork, new FakeDateTimeProvider(Now));

    private Goal StoreGoal(Guid? employeeId = null, Currency currency = Currency.ARS)
    {
        var goal = Goal.Create(employeeId ?? _employeeId, "Vacaciones", Money.From(1000m, currency), Today.AddMonths(6), Now);
        _goals.Add(goal);
        return goal;
    }

    private UpdateGoalCommand Command(Goal goal, decimal target = 2000m) =>
        new(_employeeId, goal.Id, "Viaje a Bariloche", target, Today.AddDays(10));

    [Fact]
    public async Task Editing_a_goal_returns_it_with_the_new_details_recalculated()
    {
        var goal = StoreGoal();
        goal.AddContribution(Money.From(500m, Currency.ARS), Today, null, Now);

        var result = await _handler.Handle(Command(goal), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Viaje a Bariloche");
        result.Value.TargetAmount.ShouldBe(2000m);
        result.Value.Deadline.ShouldBe(Today.AddDays(10));
        result.Value.ProgressPercentage.ShouldBe(25m);
        result.Value.RemainingAmount.ShouldBe(1500m);
        result.Value.DaysRemaining.ShouldBe(10);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_currency_of_the_goal_is_kept()
    {
        var goal = StoreGoal(currency: Currency.USD);

        var result = await _handler.Handle(Command(goal), CancellationToken.None);

        result.Value.Currency.ShouldBe(Currency.USD);
    }

    [Fact]
    public async Task Lowering_the_target_below_the_saved_amount_achieves_the_goal()
    {
        var goal = StoreGoal();
        goal.AddContribution(Money.From(600m, Currency.ARS), Today, null, Now);

        var result = await _handler.Handle(Command(goal, target: 500m), CancellationToken.None);

        result.Value.Status.ShouldBe(GoalStatus.Achieved);
        result.Value.AchievedAt.ShouldNotBeNull();
        result.Value.ProgressPercentage.ShouldBe(100m);
    }

    [Fact]
    public async Task Raising_the_target_of_an_achieved_goal_above_the_saved_amount_reactivates_it()
    {
        var goal = StoreGoal();
        goal.AddContribution(Money.From(1000m, Currency.ARS), Today, null, Now);

        var result = await _handler.Handle(Command(goal, target: 3000m), CancellationToken.None);

        result.Value.Status.ShouldBe(GoalStatus.Active);
        result.Value.AchievedAt.ShouldBeNull();
    }

    [Fact]
    public async Task A_goal_of_another_employee_is_reported_as_not_found()
    {
        var goal = StoreGoal(employeeId: Guid.CreateVersion7());

        var result = await _handler.Handle(Command(goal), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        goal.Name.ShouldBe("Vacaciones");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_deleted_goal_is_reported_as_not_found()
    {
        var goal = StoreGoal();
        goal.Cancel(Now);

        var result = await _handler.Handle(Command(goal), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _unitOfWork.SaveCount.ShouldBe(0);
    }
}

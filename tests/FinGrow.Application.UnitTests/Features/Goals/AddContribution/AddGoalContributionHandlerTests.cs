namespace FinGrow.Application.UnitTests.Features.Goals.AddContribution;

using Common;
using FinGrow.Application.Features.Goals.AddContribution;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class AddGoalContributionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeGoalRepository _goals = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AddGoalContributionHandler _handler;

    public AddGoalContributionHandlerTests() =>
        _handler = new AddGoalContributionHandler(_goals, _unitOfWork, new FakeDateTimeProvider(Now));

    private Goal StoreGoal(decimal target = 1000m, Currency currency = Currency.ARS)
    {
        var goal = Goal.Create(_employeeId, "Vacaciones", Money.From(target, currency), Today.AddMonths(6), Now);
        _goals.Add(goal);
        return goal;
    }

    private AddGoalContributionCommand Command(Guid goalId, decimal amount = 250m, Currency currency = Currency.ARS) =>
        new(_employeeId, goalId, amount, currency, Today, Note: "Aguinaldo");

    [Fact]
    public async Task A_contribution_updates_the_accumulated_amount_and_stays_in_the_history()
    {
        var goal = StoreGoal();

        var result = await _handler.Handle(Command(goal.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Goal.CurrentAmount.ShouldBe(250m);
        result.Value.Goal.ProgressPercentage.ShouldBe(25m);
        result.Value.Contribution.Amount.ShouldBe(250m);
        result.Value.Contribution.Note.ShouldBe("Aguinaldo");
        goal.Contributions.ShouldHaveSingleItem().Id.ShouldBe(result.Value.Contribution.Id);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Reaching_the_target_marks_the_goal_as_achieved()
    {
        var goal = StoreGoal(target: 1000m);

        var result = await _handler.Handle(Command(goal.Id, amount: 1000m), CancellationToken.None);

        result.Value.Goal.Status.ShouldBe(GoalStatus.Achieved);
        result.Value.Goal.ProgressPercentage.ShouldBe(100m);
    }

    [Fact]
    public async Task A_goal_of_another_employee_is_reported_as_not_found()
    {
        var foreignGoal = Goal.Create(Guid.CreateVersion7(), "Auto", Money.From(1000m, Currency.ARS), Today.AddMonths(6), Now);
        _goals.Add(foreignGoal);

        var result = await _handler.Handle(Command(foreignGoal.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        foreignGoal.Contributions.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_missing_goal_is_reported_as_not_found()
    {
        var result = await _handler.Handle(Command(Guid.CreateVersion7()), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task An_achieved_goal_rejects_new_contributions()
    {
        var goal = StoreGoal(target: 1000m);
        goal.AddContribution(Money.From(1000m, Currency.ARS), Today, null, Now);

        var result = await _handler.Handle(Command(goal.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_contribution_in_another_currency_is_rejected()
    {
        var goal = StoreGoal(currency: Currency.ARS);

        var result = await _handler.Handle(Command(goal.Id, currency: Currency.USD), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        goal.Contributions.ShouldBeEmpty();
    }
}

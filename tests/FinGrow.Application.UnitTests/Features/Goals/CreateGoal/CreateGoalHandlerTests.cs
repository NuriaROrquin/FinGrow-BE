namespace FinGrow.Application.UnitTests.Features.Goals.CreateGoal;

using FinGrow.Application.Features.Goals.CreateGoal;
using Fakes;
using Domain.Enums;

public class CreateGoalHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeGoalRepository _goals = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CreateGoalHandler _handler;

    public CreateGoalHandlerTests() =>
        _handler = new CreateGoalHandler(_goals, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task A_new_goal_is_persisted_for_the_employee_as_active_with_no_progress()
    {
        var employeeId = Guid.CreateVersion7();
        var command = new CreateGoalCommand(
            employeeId,
            Name: "Vacaciones",
            TargetAmount: 500000m,
            Currency.ARS,
            Deadline: new DateOnly(2027, 1, 15));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Vacaciones");
        result.Value.TargetAmount.ShouldBe(500000m);
        result.Value.CurrentAmount.ShouldBe(0m);
        result.Value.ProgressPercentage.ShouldBe(0m);
        result.Value.Status.ShouldBe(GoalStatus.Active);
        _unitOfWork.SaveCount.ShouldBe(1);

        var stored = _goals.Goals.ShouldHaveSingleItem();
        stored.EmployeeId.ShouldBe(employeeId);
        stored.Deadline.ShouldBe(new DateOnly(2027, 1, 15));
    }

    [Fact]
    public async Task The_name_is_stored_trimmed()
    {
        var command = new CreateGoalCommand(
            Guid.CreateVersion7(),
            Name: "  Auto  ",
            TargetAmount: 1000m,
            Currency.USD,
            Deadline: new DateOnly(2027, 6, 1));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Value.Name.ShouldBe("Auto");
    }
}

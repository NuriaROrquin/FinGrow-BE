namespace FinGrow.Application.UnitTests.Features.Goals.ListGoals;

using FinGrow.Application.Features.Goals.ListGoals;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class ListGoalsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);

    private readonly FakeGoalRepository _goals = new();

    [Fact]
    public async Task Only_the_goals_of_the_employee_are_listed_with_their_accumulated_amount()
    {
        var employeeId = Guid.CreateVersion7();
        var own = Goal.Create(employeeId, "Vacaciones", Money.From(1000m, Currency.ARS), Today.AddMonths(6), Now);
        own.AddContribution(Money.From(400m, Currency.ARS), Today, null, Now);
        _goals.Add(own);
        _goals.Add(Goal.Create(Guid.CreateVersion7(), "Auto", Money.From(1000m, Currency.ARS), Today.AddMonths(6), Now));

        var result = await new ListGoalsHandler(_goals).Handle(new ListGoalsQuery(employeeId), CancellationToken.None);

        var goal = result.Value.ShouldHaveSingleItem();
        goal.Name.ShouldBe("Vacaciones");
        goal.CurrentAmount.ShouldBe(400m);
    }
}

namespace FinGrow.Application.UnitTests.Features.Goals.ListContributions;

using Common;
using FinGrow.Application.Features.Goals.ListContributions;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class ListGoalContributionsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);
    private static readonly string[] NewestFirst = { "Ultimo", "Medio", "Primero" };

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeGoalRepository _goals = new();
    private readonly ListGoalContributionsHandler _handler;

    public ListGoalContributionsHandlerTests() => _handler = new ListGoalContributionsHandler(_goals);

    [Fact]
    public async Task The_history_is_returned_newest_first()
    {
        var goal = Goal.Create(_employeeId, "Vacaciones", Money.From(1000m, Currency.ARS), Today.AddMonths(6), Now);
        goal.AddContribution(Money.From(100m, Currency.ARS), Today.AddDays(-10), "Primero", Now);
        goal.AddContribution(Money.From(200m, Currency.ARS), Today, "Ultimo", Now);
        goal.AddContribution(Money.From(150m, Currency.ARS), Today.AddDays(-3), "Medio", Now);
        _goals.Add(goal);

        var result = await _handler.Handle(new ListGoalContributionsQuery(_employeeId, goal.Id), CancellationToken.None);

        result.Value.Select(contribution => contribution.Note).ShouldBe(NewestFirst);
    }

    [Fact]
    public async Task The_history_of_another_employee_is_not_visible()
    {
        var goal = Goal.Create(Guid.CreateVersion7(), "Auto", Money.From(1000m, Currency.ARS), Today.AddMonths(6), Now);
        _goals.Add(goal);

        var result = await _handler.Handle(new ListGoalContributionsQuery(_employeeId, goal.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}

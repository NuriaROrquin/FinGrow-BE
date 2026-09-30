namespace FinGrow.Application.UnitTests.Features.Budgets.GetBudget;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Budgets.GetBudget;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class GetBudgetHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeBudgetRepository _budgets = new();
    private readonly GetBudgetHandler _handler;

    public GetBudgetHandlerTests() => _handler = new GetBudgetHandler(_budgets);

    [Fact]
    public async Task The_budget_of_the_requested_month_is_returned_with_its_limits()
    {
        var budget = Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), Now);
        budget.SetLimit(ExpenseCategory.Transporte, Money.From(40000m, Currency.ARS), Now);
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(150000m, Currency.ARS), Now);
        _budgets.Add(budget);

        var result = await _handler.Handle(new GetBudgetQuery(EmployeeId, 2026, 9), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(budget.Id);
        result.Value.Currency.ShouldBe(Currency.ARS);
        result.Value.Limits.Select(limit => limit.Category)
            .ShouldBe(new[] { ExpenseCategory.Alimentos, ExpenseCategory.Transporte });
    }

    [Fact]
    public async Task A_month_without_a_budget_returns_not_found()
    {
        var result = await _handler.Handle(new GetBudgetQuery(EmployeeId, 2026, 9), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Budget.NotFound");
    }

    [Fact]
    public async Task Another_employee_budget_is_not_returned()
    {
        _budgets.Add(Budget.Create(Guid.CreateVersion7(), BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), Now));

        var result = await _handler.Handle(new GetBudgetQuery(EmployeeId, 2026, 9), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}

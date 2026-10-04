namespace FinGrow.Application.UnitTests.Features.Budgets.DeleteBudget;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Budgets.DeleteBudget;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class DeleteBudgetHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DeleteBudgetHandler _handler;

    public DeleteBudgetHandlerTests() => _handler = new DeleteBudgetHandler(_budgets, _unitOfWork);

    [Fact]
    public async Task The_budget_of_the_month_is_removed()
    {
        AddBudget(EmployeeId, new DateOnly(2026, 9, 1));

        var result = await _handler.Handle(new DeleteBudgetCommand(EmployeeId, 2026, 9), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _budgets.Budgets.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Only_the_requested_month_is_removed()
    {
        AddBudget(EmployeeId, new DateOnly(2026, 9, 1));
        var october = AddBudget(EmployeeId, new DateOnly(2026, 10, 1));

        await _handler.Handle(new DeleteBudgetCommand(EmployeeId, 2026, 9), CancellationToken.None);

        _budgets.Budgets.ShouldHaveSingleItem().ShouldBe(october);
    }

    [Fact]
    public async Task A_month_without_a_budget_returns_not_found()
    {
        var result = await _handler.Handle(new DeleteBudgetCommand(EmployeeId, 2026, 9), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Budget.NotFound");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Another_employee_budget_is_not_removed()
    {
        AddBudget(Guid.CreateVersion7(), new DateOnly(2026, 9, 1));

        var result = await _handler.Handle(new DeleteBudgetCommand(EmployeeId, 2026, 9), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _budgets.Budgets.Count.ShouldBe(1);
    }

    private Budget AddBudget(Guid employeeId, DateOnly month)
    {
        var budget = Budget.Create(employeeId, BudgetPeriod.Monthly, month, Now);
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(150000m, Currency.ARS), Now);
        _budgets.Add(budget);

        return budget;
    }
}

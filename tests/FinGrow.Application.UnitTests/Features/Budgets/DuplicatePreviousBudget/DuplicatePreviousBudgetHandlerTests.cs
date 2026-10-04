namespace FinGrow.Application.UnitTests.Features.Budgets.DuplicatePreviousBudget;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Budgets.DuplicatePreviousBudget;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class DuplicatePreviousBudgetHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DuplicatePreviousBudgetHandler _handler;

    public DuplicatePreviousBudgetHandlerTests() =>
        _handler = new DuplicatePreviousBudgetHandler(_budgets, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task The_previous_month_budget_is_copied_into_the_requested_month()
    {
        var september = SeptemberBudget();
        _budgets.Add(september);

        var result = await _handler.Handle(new DuplicatePreviousBudgetCommand(EmployeeId, 2026, 10), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(september.Id);
        result.Value.PeriodStart.ShouldBe(new DateOnly(2026, 10, 1));
        result.Value.Currency.ShouldBe(Currency.ARS);
        _unitOfWork.SaveCount.ShouldBe(1);

        var october = _budgets.Budgets.Single(budget => budget.PeriodStart == new DateOnly(2026, 10, 1));
        october.EmployeeId.ShouldBe(EmployeeId);
        october.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(150000m);
        october.LimitFor(ExpenseCategory.Transporte)!.Amount.ShouldBe(40000m);
    }

    [Fact]
    public async Task January_duplicates_the_december_of_the_previous_year()
    {
        var december = Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 12, 1), Now);
        december.SetLimit(ExpenseCategory.Entretenimiento, Money.From(80000m, Currency.ARS), Now);
        _budgets.Add(december);

        var result = await _handler.Handle(new DuplicatePreviousBudgetCommand(EmployeeId, 2027, 1), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.PeriodStart.ShouldBe(new DateOnly(2027, 1, 1));
        result.Value.Limits.ShouldHaveSingleItem().Category.ShouldBe(ExpenseCategory.Entretenimiento);
    }

    [Fact]
    public async Task Without_a_previous_month_budget_it_returns_not_found()
    {
        var result = await _handler.Handle(new DuplicatePreviousBudgetCommand(EmployeeId, 2026, 10), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Budget.PreviousNotFound");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Another_employee_budget_is_not_used_as_the_previous_month()
    {
        var foreign = Budget.Create(Guid.CreateVersion7(), BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), Now);
        foreign.SetLimit(ExpenseCategory.Alimentos, Money.From(1000m, Currency.ARS), Now);
        _budgets.Add(foreign);

        var result = await _handler.Handle(new DuplicatePreviousBudgetCommand(EmployeeId, 2026, 10), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task If_the_requested_month_already_has_a_budget_it_returns_a_conflict()
    {
        _budgets.Add(SeptemberBudget());
        _budgets.Add(Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 10, 1), Now));

        var result = await _handler.Handle(new DuplicatePreviousBudgetCommand(EmployeeId, 2026, 10), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Budget.AlreadyExists");
        _budgets.Budgets.Count.ShouldBe(2);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    private static Budget SeptemberBudget()
    {
        var budget = Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), Now.AddMonths(-1));
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(150000m, Currency.ARS), Now.AddMonths(-1));
        budget.SetLimit(ExpenseCategory.Transporte, Money.From(40000m, Currency.ARS), Now.AddMonths(-1));

        return budget;
    }
}

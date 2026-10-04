namespace FinGrow.Application.UnitTests.Features.Budgets.SetCategoryLimit;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Budgets.SetCategoryLimit;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class SetBudgetCategoryLimitHandlerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeBudgetSpendingReadRepository _spending = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly SetBudgetCategoryLimitHandler _handler;

    public SetBudgetCategoryLimitHandlerTests() =>
        _handler = new SetBudgetCategoryLimitHandler(_budgets, _spending, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task Editing_an_existing_limit_saves_the_new_amount()
    {
        var budget = SeptemberBudget();

        var result = await _handler.Handle(Command(ExpenseCategory.Alimentos, 180000m), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        budget.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(180000m);
        budget.Limits.Count.ShouldBe(2);
        budget.UpdatedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_category_without_a_limit_gets_one_in_the_budget_currency()
    {
        var budget = SeptemberBudget();

        var result = await _handler.Handle(Command(ExpenseCategory.Salud, 30000m), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        budget.Limits.Count.ShouldBe(3);
        budget.LimitFor(ExpenseCategory.Salud).ShouldBe(Money.From(30000m, Currency.ARS));
    }

    [Fact]
    public async Task The_response_is_recalculated_against_the_new_limit()
    {
        SeptemberBudget();
        _spending.Spent[ExpenseCategory.Alimentos] = 120000m;

        var result = await _handler.Handle(Command(ExpenseCategory.Alimentos, 100000m), CancellationToken.None);

        var food = result.Value.Limits.Single(limit => limit.Category == ExpenseCategory.Alimentos);
        food.Amount.ShouldBe(100000m);
        food.Spent.ShouldBe(120000m);
        food.Remaining.ShouldBe(-20000m);
        food.UsedPercentage.ShouldBe(120m);
        food.Health.ShouldBe(BudgetHealth.Exceeded);
    }

    [Fact]
    public async Task A_month_without_a_budget_returns_not_found()
    {
        var result = await _handler.Handle(Command(ExpenseCategory.Alimentos, 100000m), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Budget.NotFound");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Another_employee_budget_is_not_touched()
    {
        var foreign = Budget.Create(Guid.CreateVersion7(), BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), CreatedAt);
        foreign.SetLimit(ExpenseCategory.Alimentos, Money.From(150000m, Currency.ARS), CreatedAt);
        _budgets.Add(foreign);

        var result = await _handler.Handle(Command(ExpenseCategory.Alimentos, 1m), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        foreign.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(150000m);
    }

    private static SetBudgetCategoryLimitCommand Command(ExpenseCategory category, decimal amount) =>
        new(EmployeeId, 2026, 9, category, amount);

    private Budget SeptemberBudget()
    {
        var budget = Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), CreatedAt);
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(150000m, Currency.ARS), CreatedAt);
        budget.SetLimit(ExpenseCategory.Transporte, Money.From(40000m, Currency.ARS), CreatedAt);
        _budgets.Add(budget);

        return budget;
    }
}

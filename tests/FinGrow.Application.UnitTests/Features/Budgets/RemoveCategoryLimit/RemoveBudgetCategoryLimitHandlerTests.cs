namespace FinGrow.Application.UnitTests.Features.Budgets.RemoveCategoryLimit;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Budgets.RemoveCategoryLimit;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class RemoveBudgetCategoryLimitHandlerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeBudgetSpendingReadRepository _spending = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly RemoveBudgetCategoryLimitHandler _handler;

    public RemoveBudgetCategoryLimitHandlerTests() =>
        _handler = new RemoveBudgetCategoryLimitHandler(_budgets, _spending, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task The_category_is_taken_out_of_the_budget()
    {
        var budget = BudgetWith(ExpenseCategory.Alimentos, ExpenseCategory.Transporte);

        var result = await _handler.Handle(Command(ExpenseCategory.Transporte), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        budget.LimitFor(ExpenseCategory.Transporte).ShouldBeNull();
        budget.UpdatedAt.ShouldBe(Now);
        result.Value.Limits.ShouldHaveSingleItem().Category.ShouldBe(ExpenseCategory.Alimentos);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_remaining_categories_come_with_what_was_spent()
    {
        BudgetWith(ExpenseCategory.Alimentos, ExpenseCategory.Transporte);
        _spending.Spent[ExpenseCategory.Alimentos] = 30000m;

        var result = await _handler.Handle(Command(ExpenseCategory.Transporte), CancellationToken.None);

        var food = result.Value.Limits.ShouldHaveSingleItem();
        food.Spent.ShouldBe(30000m);
        food.Health.ShouldBe(BudgetHealth.OnTrack);
    }

    [Fact]
    public async Task The_last_category_cannot_be_removed()
    {
        var budget = BudgetWith(ExpenseCategory.Alimentos);

        var result = await _handler.Handle(Command(ExpenseCategory.Alimentos), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Budget.LastLimit");
        budget.Limits.Count.ShouldBe(1);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_category_without_a_limit_returns_not_found()
    {
        BudgetWith(ExpenseCategory.Alimentos, ExpenseCategory.Transporte);

        var result = await _handler.Handle(Command(ExpenseCategory.Salud), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Budget.LimitNotFound");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_month_without_a_budget_returns_not_found()
    {
        var result = await _handler.Handle(Command(ExpenseCategory.Alimentos), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Budget.NotFound");
    }

    private static RemoveBudgetCategoryLimitCommand Command(ExpenseCategory category) =>
        new(EmployeeId, 2026, 9, category);

    private Budget BudgetWith(params ExpenseCategory[] categories)
    {
        var budget = Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), CreatedAt);

        foreach (var category in categories)
        {
            budget.SetLimit(category, Money.From(100000m, Currency.ARS), CreatedAt);
        }

        _budgets.Add(budget);

        return budget;
    }
}

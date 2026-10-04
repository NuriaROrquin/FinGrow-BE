namespace FinGrow.Application.UnitTests.Features.Budgets.ChangeCurrency;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Budgets.ChangeCurrency;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class ChangeBudgetCurrencyHandlerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeBudgetSpendingReadRepository _spending = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ChangeBudgetCurrencyHandler _handler;

    public ChangeBudgetCurrencyHandlerTests() =>
        _handler = new ChangeBudgetCurrencyHandler(_budgets, _spending, _unitOfWork, new FakeDateTimeProvider(Now));

    [Theory]
    [InlineData(Currency.USD)]
    [InlineData(Currency.EUR)]
    [InlineData(Currency.BRL)]
    public async Task Every_limit_moves_to_the_new_currency_keeping_its_amount(Currency currency)
    {
        var budget = SeptemberBudgetInPesos();

        var result = await _handler.Handle(Command(currency), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Currency.ShouldBe(currency);
        budget.LimitFor(ExpenseCategory.Alimentos).ShouldBe(Money.From(150000m, currency));
        budget.LimitFor(ExpenseCategory.Transporte).ShouldBe(Money.From(40000m, currency));
        budget.UpdatedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_response_comes_with_what_was_spent_in_the_new_currency()
    {
        SeptemberBudgetInPesos();
        _spending.Spent[ExpenseCategory.Alimentos] = 200m;

        var result = await _handler.Handle(Command(Currency.USD), CancellationToken.None);

        var food = result.Value.Limits.Single(limit => limit.Category == ExpenseCategory.Alimentos);
        food.Amount.ShouldBe(150000m);
        food.Spent.ShouldBe(200m);
        food.Health.ShouldBe(BudgetHealth.OnTrack);
    }

    [Fact]
    public async Task Asking_for_the_currency_it_already_has_changes_nothing()
    {
        var budget = SeptemberBudgetInPesos();

        var result = await _handler.Handle(Command(Currency.ARS), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        budget.UpdatedAt.ShouldBe(CreatedAt);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_month_without_a_budget_returns_not_found()
    {
        var result = await _handler.Handle(Command(Currency.USD), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Budget.NotFound");
    }

    [Fact]
    public async Task Another_employee_budget_is_not_touched()
    {
        var foreign = Budget.Create(Guid.CreateVersion7(), BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), CreatedAt);
        foreign.SetLimit(ExpenseCategory.Alimentos, Money.From(150000m, Currency.ARS), CreatedAt);
        _budgets.Add(foreign);

        var result = await _handler.Handle(Command(Currency.USD), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        foreign.Currency.ShouldBe(Currency.ARS);
    }

    private static ChangeBudgetCurrencyCommand Command(Currency currency) => new(EmployeeId, 2026, 9, currency);

    private Budget SeptemberBudgetInPesos()
    {
        var budget = Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), CreatedAt);
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(150000m, Currency.ARS), CreatedAt);
        budget.SetLimit(ExpenseCategory.Transporte, Money.From(40000m, Currency.ARS), CreatedAt);
        _budgets.Add(budget);

        return budget;
    }
}

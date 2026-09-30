namespace FinGrow.Application.UnitTests.Features.Budgets.CreateBudget;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Budgets.CreateBudget;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class CreateBudgetHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CreateBudgetHandler _handler;

    public CreateBudgetHandlerTests() =>
        _handler = new CreateBudgetHandler(_budgets, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task A_new_budget_is_persisted_for_the_employee_and_the_month_with_its_limits()
    {
        var result = await _handler.Handle(SeptemberCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Period.ShouldBe(BudgetPeriod.Monthly);
        result.Value.PeriodStart.ShouldBe(new DateOnly(2026, 9, 1));
        result.Value.PeriodEnd.ShouldBe(new DateOnly(2026, 9, 30));
        result.Value.Currency.ShouldBe(Currency.ARS);
        result.Value.Limits.Count.ShouldBe(2);
        _unitOfWork.SaveCount.ShouldBe(1);

        var stored = _budgets.Budgets.ShouldHaveSingleItem();
        stored.EmployeeId.ShouldBe(EmployeeId);
        stored.PeriodStart.ShouldBe(new DateOnly(2026, 9, 1));
        stored.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(150000m);
        stored.LimitFor(ExpenseCategory.Transporte)!.Amount.ShouldBe(40000m);
    }

    [Fact]
    public async Task Creating_a_second_budget_for_the_same_month_returns_a_conflict_and_saves_nothing()
    {
        var existing = Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 9, 10), Now);
        existing.SetLimit(ExpenseCategory.Alimentos, Money.From(100000m, Currency.ARS), Now);
        _budgets.Add(existing);

        var result = await _handler.Handle(SeptemberCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Budget.AlreadyExists");
        _budgets.Budgets.ShouldHaveSingleItem().ShouldBe(existing);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Another_employee_can_create_a_budget_for_the_same_month()
    {
        _budgets.Add(Budget.Create(Guid.CreateVersion7(), BudgetPeriod.Monthly, new DateOnly(2026, 9, 1), Now));

        var result = await _handler.Handle(SeptemberCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _budgets.Budgets.Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_budget_for_a_different_month_does_not_conflict()
    {
        _budgets.Add(Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 8, 1), Now));

        var result = await _handler.Handle(SeptemberCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    private static CreateBudgetCommand SeptemberCommand() => new(
        EmployeeId,
        Year: 2026,
        Month: 9,
        Currency.ARS,
        new[]
        {
            new CategoryLimitInput(ExpenseCategory.Alimentos, 150000m),
            new CategoryLimitInput(ExpenseCategory.Transporte, 40000m),
        });
}

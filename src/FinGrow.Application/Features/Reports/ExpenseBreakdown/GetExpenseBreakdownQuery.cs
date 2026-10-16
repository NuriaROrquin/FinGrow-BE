namespace FinGrow.Application.Features.Reports.ExpenseBreakdown;

using Common;
using Domain.Enums;
using MediatR;

public sealed record GetExpenseBreakdownQuery(Guid EmployeeId, Currency Currency, DateOnly From, DateOnly To)
    : IRequest<Result<ExpenseBreakdownResponse>>;

public sealed record ExpenseBreakdownResponse(
    Currency Currency,
    DateOnly From,
    DateOnly To,
    DateOnly PreviousFrom,
    DateOnly PreviousTo,
    bool HasPreviousPeriod,
    IReadOnlyList<CategoryExpense> Categories,
    ExpenseTotal Consumption,
    ExpenseTotal Savings,
    ExpenseTotal Total);

public sealed record CategoryExpense(
    ExpenseCategory Category,
    decimal Amount,
    decimal Percentage,
    ExpenseComparison? Comparison);

public sealed record ExpenseTotal(decimal Amount, ExpenseComparison? Comparison);

public sealed record ExpenseComparison(
    decimal PreviousAmount,
    decimal VariationAmount,
    decimal? VariationPercent,
    bool IsNew);

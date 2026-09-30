namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record BudgetResponse(
    Guid Id,
    BudgetPeriod Period,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    Currency? Currency,
    IReadOnlyList<BudgetLimitResponse> Limits,
    DateTimeOffset CreatedAt)
{
    public static BudgetResponse FromEntity(Budget budget) => new(
        budget.Id,
        budget.Period,
        budget.PeriodStart,
        budget.PeriodEnd,
        budget.Currency,
        budget.Limits
            .OrderBy(limit => limit.Category)
            .Select(limit => new BudgetLimitResponse(limit.Category, limit.Limit.Amount))
            .ToList(),
        budget.CreatedAt);
}

public sealed record BudgetLimitResponse(ExpenseCategory Category, decimal Amount);

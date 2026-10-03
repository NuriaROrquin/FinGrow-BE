namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public sealed record BudgetResponse(
    Guid Id,
    BudgetPeriod Period,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    Currency? Currency,
    IReadOnlyList<BudgetLimitResponse> Limits,
    DateTimeOffset CreatedAt)
{
    public static BudgetResponse FromEntity(
        Budget budget,
        IReadOnlyDictionary<ExpenseCategory, decimal>? spentByCategory = null) => new(
        budget.Id,
        budget.Period,
        budget.PeriodStart,
        budget.PeriodEnd,
        budget.Currency,
        budget.Limits
            .OrderBy(limit => limit.Category)
            .Select(limit => spentByCategory is null
                ? new BudgetLimitResponse(limit.Category, limit.Limit.Amount)
                : BudgetLimitResponse.WithSpending(budget, limit, spentByCategory.GetValueOrDefault(limit.Category)))
            .ToList(),
        budget.CreatedAt);
}

public sealed record BudgetLimitResponse(
    ExpenseCategory Category,
    decimal Amount,
    decimal? Spent = null,
    decimal? Remaining = null,
    decimal? UsedPercentage = null,
    BudgetHealth? Health = null)
{
    public static BudgetLimitResponse WithSpending(Budget budget, BudgetCategoryLimit limit, decimal spentAmount)
    {
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentNullException.ThrowIfNull(limit);

        var spent = Money.From(spentAmount, limit.Limit.Currency);

        return new BudgetLimitResponse(
            limit.Category,
            limit.Limit.Amount,
            spent.Amount,
            limit.Limit.DifferenceWith(spent),
            decimal.Round(spent.Amount * 100m / limit.Limit.Amount, 2, MidpointRounding.ToEven),
            budget.Evaluate(limit.Category, spent));
    }
}

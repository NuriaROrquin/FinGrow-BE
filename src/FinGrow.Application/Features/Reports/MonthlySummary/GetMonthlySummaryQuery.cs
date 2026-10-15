namespace FinGrow.Application.Features.Reports.MonthlySummary;

using Common;
using Domain.Enums;
using MediatR;

public sealed record GetMonthlySummaryQuery(Guid EmployeeId, Currency Currency, DateOnly From, DateOnly To)
    : IRequest<Result<MonthlySummaryResponse>>;

public sealed record MonthlySummaryResponse(
    Currency Currency,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<MonthlySummaryMonth> Months,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal TotalSavings,
    decimal? AverageMonthlySavings,
    IReadOnlyList<CategoryAmount> ExpensesByCategory,
    MonthHighlights Highlights);

public sealed record MonthlySummaryMonth(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    decimal Savings,
    IReadOnlyList<CategoryAmount> ExpensesByCategory)
{
    public bool HasMovements => Income != 0m || Expenses != 0m;
}

public sealed record CategoryAmount(ExpenseCategory Category, decimal Amount);

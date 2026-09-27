namespace FinGrow.Application.Features.Reports.SavingsVsGoals;

using Common;
using Domain.Enums;
using MediatR;

public sealed record GetSavingsVsGoalsQuery(Guid EmployeeId, Currency Currency, int? Months)
    : IRequest<Result<SavingsVsGoalsResponse>>
{
    public const int MaxMonths = 36;
}

public sealed record SavingsVsGoalsResponse(
    Currency Currency,
    IReadOnlyList<SavingsVsGoalsMonth> Months,
    decimal TotalIncome,
    decimal TotalActualSavings,
    decimal TotalCommitted,
    int MonthsWithCommitment,
    int MonthsOnTarget,
    decimal? SavingsRate,
    bool HasGoals);

/// <param name="ActualSavings">Ingresos menos gastos confirmados del mes; puede ser negativo.</param>
/// <param name="Committed">Suma de la cuota mensual de las metas vigentes ese mes.</param>
/// <param name="MetTarget">Null si ese mes no habia ninguna meta comprometida.</param>
public sealed record SavingsVsGoalsMonth(
    int Year,
    int Month,
    decimal Income,
    decimal Expense,
    decimal ActualSavings,
    decimal Committed,
    bool? MetTarget);

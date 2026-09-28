namespace FinGrow.Application.Features.Reports.SavingsVsGoals;

using Common;
using Domain.Enums;
using MediatR;

public sealed record GetSavingsVsGoalsQuery(Guid EmployeeId, Currency Currency, int? Months)
    : IRequest<Result<SavingsVsGoalsResponse>>
{
    public const int MaxMonths = 36;
}

/// <param name="CompletionRate">Aportado sobre comprometido en el periodo, en %; null si no habia nada comprometido.</param>
public sealed record SavingsVsGoalsResponse(
    Currency Currency,
    IReadOnlyList<SavingsVsGoalsMonth> Months,
    decimal TotalContributed,
    decimal TotalCommitted,
    int MonthsWithCommitment,
    int MonthsOnTarget,
    decimal? CompletionRate,
    bool HasGoals);

/// <param name="Contributed">Suma de los aportes con fecha en ese mes, en todas las metas de la moneda.</param>
/// <param name="Committed">Suma de la cuota mensual de las metas vigentes ese mes.</param>
/// <param name="MetTarget">
/// Null si ese mes no habia ninguna meta comprometida, o si es el mes en curso y todavia no se llego a la cuota.
/// </param>
public sealed record SavingsVsGoalsMonth(
    int Year,
    int Month,
    decimal Contributed,
    decimal Committed,
    bool? MetTarget);

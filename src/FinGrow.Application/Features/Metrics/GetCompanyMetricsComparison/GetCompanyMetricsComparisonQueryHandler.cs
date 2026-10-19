namespace FinGrow.Application.Features.Metrics.GetCompanyMetricsComparison;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetCompanyMetricsComparisonQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IMetricsSnapshotRepository snapshots,
    MetricsAnonymizationPolicy anonymizationPolicy)
    : IRequestHandler<GetCompanyMetricsComparisonQuery, Result<CompanyMetricsComparisonResponse>>
{
    public async Task<Result<CompanyMetricsComparisonResponse>> Handle(
        GetCompanyMetricsComparisonQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure<CompanyMetricsComparisonResponse>(
                Error.Forbidden("Metrics.Unauthenticated", "Solo una empresa puede consultar sus metricas."));
        }

        if (await companies.GetByIdAsync(companyId, cancellationToken) is null)
        {
            return Result.Failure<CompanyMetricsComparisonResponse>(
                Error.NotFound("Metrics.CompanyNotFound", "No existe la empresa solicitada."));
        }

        var companySnapshots = await snapshots.ListCompanyAsync(
            companyId, request.FromDate, request.ToDate, cancellationToken);

        if (companySnapshots.Any(snapshot => !anonymizationPolicy.CanRevealCompany(snapshot.Metrics)))
        {
            return Result.Failure<CompanyMetricsComparisonResponse>(
                MetricsAnonymizationPolicy.NotEnoughEmployees);
        }

        if (companySnapshots.Count < 2)
        {
            return Result.Failure<CompanyMetricsComparisonResponse>(MetricsErrors.NotEnoughPeriods);
        }

        var periods = companySnapshots
            .Select(snapshot => new CompanyMetricsComparisonPeriodResponse(
                snapshot.PeriodStart,
                snapshot.Metrics.ParticipationRate,
                snapshot.Metrics.AverageFinancialHealthScore))
            .ToList();

        var first = periods[0];
        var last = periods[^1];

        return Result.Success(new CompanyMetricsComparisonResponse(
            periods,
            ChangeBetween(first.ParticipationRate, last.ParticipationRate),
            ChangeBetween(first.AverageFinancialHealthScore, last.AverageFinancialHealthScore)));
    }

    private static decimal? ChangeBetween(decimal? first, decimal? last) =>
        first is { } firstValue && last is { } lastValue
            ? decimal.Round(lastValue - firstValue, 2, MidpointRounding.ToEven)
            : null;
}

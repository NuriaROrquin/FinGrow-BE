namespace FinGrow.Application.Features.Metrics.GetCompanyTrends;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics.Trends;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class GetCompanyTrendsQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IMetricsSnapshotRepository snapshots,
    IFinancialHealthScoreRepository scores,
    ICompanyEducationMetricsReadRepository education,
    MetricsAnonymizationPolicy anonymizationPolicy)
    : IRequestHandler<GetCompanyTrendsQuery, Result<CompanyTrendsResponse>>
{
    public async Task<Result<CompanyTrendsResponse>> Handle(
        GetCompanyTrendsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure<CompanyTrendsResponse>(
                Error.Forbidden("Metrics.Unauthenticated", "Solo una empresa puede consultar sus metricas."));
        }

        if (await companies.GetByIdAsync(companyId, cancellationToken) is null)
        {
            return Result.Failure<CompanyTrendsResponse>(
                Error.NotFound("Metrics.CompanyNotFound", "No existe la empresa solicitada."));
        }

        var minimumGroupSize = anonymizationPolicy.CompanyMinimumEmployees;
        var periods = await snapshots.ListCompanyPeriodStartsAsync(companyId, cancellationToken);

        if (periods.Count == 0)
        {
            return Result.Success(CompanyTrendsResponse.WithoutPeriod(minimumGroupSize));
        }

        var period = MetricsPeriod.FromStart(periods.Max());
        var previousPeriod = period.Previous();
        var current = await snapshots.FindCompanyAsync(companyId, period.Start, cancellationToken);
        var previous = await snapshots.FindCompanyAsync(companyId, previousPeriod.Start, cancellationToken);

        if (current is null)
        {
            return Result.Success(CompanyTrendsResponse.WithoutPeriod(minimumGroupSize));
        }

        if (previous is null)
        {
            return Result.Success(CompanyTrendsResponse.WithoutPreviousPeriod(period.Start, minimumGroupSize));
        }

        if (!anonymizationPolicy.CanRevealCompany(current.Metrics)
            || !anonymizationPolicy.CanRevealCompany(previous.Metrics))
        {
            return Result.Success(
                CompanyTrendsResponse.Anonymized(period.Start, previousPeriod.Start, minimumGroupSize));
        }

        var trends = CompanyTrendsCalculator.Compare(
            await ReadAsync(companyId, previous, cancellationToken),
            await ReadAsync(companyId, current, cancellationToken));

        return Result.Success(new CompanyTrendsResponse(
            period.Start,
            previousPeriod.Start,
            false,
            minimumGroupSize,
            trends.Positive,
            trends.Opportunities));
    }

    private async Task<IReadOnlyDictionary<CompanyTrendIndicator, decimal>> ReadAsync(
        Guid companyId,
        CompanyMetricsSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var monthScores = await scores.ListByCompanyAsync(companyId, snapshot.PeriodStart, cancellationToken);
        var completedCourses = await education.CountCompletedCoursesAsync(companyId, snapshot.Period, cancellationToken);

        return CompanyMonthIndicators.Read(
            snapshot.Metrics,
            monthScores.Select(score => score.Factors).ToList(),
            completedCourses,
            anonymizationPolicy);
    }
}

namespace FinGrow.Application.Features.Metrics.GetCompanyEducationMetrics;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class GetCompanyEducationMetricsQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IEmployeeRepository employees,
    ICompanyEducationMetricsReadRepository education,
    IDateTimeProvider clock,
    MetricsAnonymizationPolicy anonymizationPolicy)
    : IRequestHandler<GetCompanyEducationMetricsQuery, Result<CompanyEducationMetricsResponse>>
{
    public async Task<Result<CompanyEducationMetricsResponse>> Handle(
        GetCompanyEducationMetricsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure<CompanyEducationMetricsResponse>(
                Error.Forbidden("Metrics.Unauthenticated", "Solo una empresa puede consultar sus metricas."));
        }

        if (await companies.GetByIdAsync(companyId, cancellationToken) is null)
        {
            return Result.Failure<CompanyEducationMetricsResponse>(
                Error.NotFound("Metrics.CompanyNotFound", "No existe la empresa solicitada."));
        }

        var activeEmployees = (await employees.ListByCompanyAsync(companyId, cancellationToken))
            .Count(employee => employee.IsActive);

        if (!anonymizationPolicy.CanRevealCompany(activeEmployees))
        {
            return Result.Failure<CompanyEducationMetricsResponse>(
                MetricsAnonymizationPolicy.NotEnoughEmployees);
        }

        var today = ArgentinaTime.DateOf(clock.UtcNow);
        var metrics = await education.GetAsync(
            companyId,
            today.AddDays(-29),
            cancellationToken);

        return Result.Success(new CompanyEducationMetricsResponse(
            metrics.TotalCompletedCourses,
            SatisfactionPercentage(metrics),
            metrics.Last30DaysCompletedCourses));
    }

    private static decimal? SatisfactionPercentage(CompanyEducationMetrics metrics) =>
        metrics.CourseRatingsCount == 0
            ? null
            : decimal.Round(
                (decimal)metrics.CourseRatingSum
                * 100m
                / (metrics.CourseRatingsCount * CourseRating.MaxScore),
                2,
                MidpointRounding.ToEven);
}

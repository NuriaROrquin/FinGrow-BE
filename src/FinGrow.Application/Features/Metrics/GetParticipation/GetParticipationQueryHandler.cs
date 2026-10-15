namespace FinGrow.Application.Features.Metrics.GetParticipation;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Metrics.SnapshotMetrics;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Common;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class GetParticipationQueryHandler
    : IRequestHandler<GetParticipationQuery, Result<CompanyParticipationResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly IPeriodActivityReadRepository _activity;
    private readonly IDateTimeProvider _clock;
    private readonly MetricsAnonymizationPolicy _anonymizationPolicy;

    public GetParticipationQueryHandler(
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        IPeriodActivityReadRepository activity,
        IDateTimeProvider clock,
        MetricsAnonymizationPolicy anonymizationPolicy)
    {
        _currentUser = currentUser;
        _employees = employees;
        _activity = activity;
        _clock = clock;
        _anonymizationPolicy = anonymizationPolicy;
    }

    public async Task<Result<CompanyParticipationResponse>> Handle(
        GetParticipationQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure<CompanyParticipationResponse>(MetricsErrors.NoAutenticado);
        }

        var period = MetricsPeriod.MonthOf(ArgentinaTime.DateOf(_clock.UtcNow));
        var employees = await _employees.ListByCompanyAsync(companyId, cancellationToken);
        var activity = await _activity.ListByCompanyAsync(companyId, period, cancellationToken);
        var metrics = PeriodMetricsCalculator.Aggregate(activity, new Dictionary<Guid, int>());

        return Result.Success(CompanyParticipationResponse.From(
            period,
            employees.Count,
            metrics,
            _anonymizationPolicy.CanRevealCompany(metrics),
            _anonymizationPolicy.CompanyMinimumEmployees));
    }
}

namespace FinGrow.Application.Features.Metrics.GetDepartmentMetricsOverview;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetDepartmentMetricsOverviewQuery
    : IRequest<Result<IReadOnlyList<DepartmentMetricsOverviewResponse>>>;

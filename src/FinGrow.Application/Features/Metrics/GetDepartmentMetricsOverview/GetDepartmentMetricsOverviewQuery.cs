namespace FinGrow.Application.Features.Metrics.GetDepartmentMetricsOverview;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetDepartmentMetricsOverviewQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null)
    : IRequest<Result<IReadOnlyList<DepartmentMetricsOverviewResponse>>>;

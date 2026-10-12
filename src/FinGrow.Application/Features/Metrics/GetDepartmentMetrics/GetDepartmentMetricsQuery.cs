namespace FinGrow.Application.Features.Metrics.GetDepartmentMetrics;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetDepartmentMetricsQuery(
    Guid DepartmentId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<Result<IReadOnlyList<DepartmentMetricsResponse>>>;


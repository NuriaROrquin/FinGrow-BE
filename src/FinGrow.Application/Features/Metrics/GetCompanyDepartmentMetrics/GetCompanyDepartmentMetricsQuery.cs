namespace FinGrow.Application.Features.Metrics.GetCompanyDepartmentMetrics;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetCompanyDepartmentMetricsQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null)
    : IRequest<Result<IReadOnlyList<CompanyDepartmentMetricsResponse>>>;

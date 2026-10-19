namespace FinGrow.Application.Features.Metrics.GetCompanyMetricsComparison;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetCompanyMetricsComparisonQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<Result<CompanyMetricsComparisonResponse>>;

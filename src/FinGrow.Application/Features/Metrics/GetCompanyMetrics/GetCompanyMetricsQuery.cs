namespace FinGrow.Application.Features.Metrics.GetCompanyMetrics;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetCompanyMetricsQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<Result<IReadOnlyList<CompanyMetricsResponse>>>;


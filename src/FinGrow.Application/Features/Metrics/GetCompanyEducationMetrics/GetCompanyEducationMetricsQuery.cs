namespace FinGrow.Application.Features.Metrics.GetCompanyEducationMetrics;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetCompanyEducationMetricsQuery : IRequest<Result<CompanyEducationMetricsResponse>>;

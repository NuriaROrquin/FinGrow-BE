namespace FinGrow.Application.Features.Metrics.GetCompanyTrends;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetCompanyTrendsQuery : IRequest<Result<CompanyTrendsResponse>>;

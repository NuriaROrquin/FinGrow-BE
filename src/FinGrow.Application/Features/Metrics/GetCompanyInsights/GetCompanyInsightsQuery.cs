namespace FinGrow.Application.Features.Metrics.GetCompanyInsights;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetCompanyInsightsQuery : IRequest<Result<CompanyInsightsResponse>>;

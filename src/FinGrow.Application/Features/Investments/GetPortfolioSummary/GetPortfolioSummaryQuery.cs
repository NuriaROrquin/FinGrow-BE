namespace FinGrow.Application.Features.Investments.GetPortfolioSummary;

using Common;
using DTOs;
using MediatR;

public sealed record GetPortfolioSummaryQuery(Guid EmployeeId) : IRequest<Result<PortfolioSummaryResponse>>;

namespace FinGrow.Application.Features.Investments.GetPortfolioEvolution;

using Common;
using Domain.Enums;
using MediatR;

public sealed record GetPortfolioEvolutionQuery(Guid EmployeeId, Currency Currency, PortfolioEvolutionRange Range)
    : IRequest<Result<PortfolioEvolutionResponse>>;

public sealed record PortfolioEvolutionResponse(
    Currency Currency,
    PortfolioEvolutionInterval Interval,
    DateOnly? HistoryStartsOn,
    IReadOnlyList<PortfolioEvolutionPoint> Points);

public sealed record PortfolioEvolutionPoint(DateOnly Date, decimal Value, decimal InvestedAmount);

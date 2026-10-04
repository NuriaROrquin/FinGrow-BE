namespace FinGrow.Application.Features.Investments;

using Domain.Enums;

public interface IInvestmentTracking
{
    InvestmentType Type { get; }

    string? Symbol { get; }

    decimal? Quantity { get; }
}

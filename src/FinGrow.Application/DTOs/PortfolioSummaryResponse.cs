namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record CurrencyPortfolioResponse(
    Currency Currency,
    int InvestmentCount,
    decimal InvestedAmount,
    decimal CurrentValue,
    int QuotedCount,
    decimal QuotedInvestedAmount,
    decimal QuotedReturnAmount,
    decimal QuotedReturnPercentage)
{
    public static CurrencyPortfolioResponse FromGroup(IGrouping<Currency, Investment> group)
    {
        var quoted = group.Where(investment => investment.HasMarketValuation).ToList();
        var quotedInvested = quoted.Sum(investment => investment.InvestedAmount.Amount);
        var quotedReturn = quoted.Sum(investment => investment.ReturnAmount);

        return new CurrencyPortfolioResponse(
            group.Key,
            group.Count(),
            group.Sum(investment => investment.InvestedAmount.Amount),
            group.Sum(investment => investment.CurrentValue.Amount),
            quoted.Count,
            quotedInvested,
            quotedReturn,
            quotedInvested == 0m
                ? 0m
                : decimal.Round(quotedReturn / quotedInvested * 100m, 2, MidpointRounding.ToEven));
    }
}

public sealed record AllocationGroupResponse(InvestmentType Type, Currency Currency, decimal CurrentValue);

public sealed record LastPurchaseResponse(Guid Id, string AssetName, InvestmentType Type, DateOnly PurchasedOn);

public sealed record PortfolioSummaryResponse(
    int InvestmentCount,
    int UnquotedCount,
    DateOnly? OldestQuotedOn,
    LastPurchaseResponse? LastPurchase,
    IReadOnlyList<CurrencyPortfolioResponse> Currencies,
    IReadOnlyList<AllocationGroupResponse> Allocation)
{
    public static PortfolioSummaryResponse FromInvestments(IReadOnlyCollection<Investment> investments)
    {
        var quoted = investments.Where(investment => investment.HasMarketValuation).ToList();

        var currencies = investments
            .GroupBy(investment => investment.InvestedAmount.Currency)
            .OrderBy(group => group.Key)
            .Select(CurrencyPortfolioResponse.FromGroup)
            .ToList();

        var allocation = investments
            .GroupBy(investment => new { investment.Type, investment.InvestedAmount.Currency })
            .OrderBy(group => group.Key.Type)
            .ThenBy(group => group.Key.Currency)
            .Select(group => new AllocationGroupResponse(
                group.Key.Type,
                group.Key.Currency,
                group.Sum(investment => investment.CurrentValue.Amount)))
            .ToList();

        var lastPurchase = investments
            .OrderByDescending(investment => investment.PurchasedOn)
            .ThenByDescending(investment => investment.CreatedAt)
            .Select(investment => new LastPurchaseResponse(
                investment.Id,
                investment.AssetName,
                investment.Type,
                investment.PurchasedOn))
            .FirstOrDefault();

        return new PortfolioSummaryResponse(
            investments.Count,
            investments.Count - quoted.Count,
            quoted.Count == 0 ? null : quoted.Min(investment => investment.ValuedOn),
            lastPurchase,
            currencies,
            allocation);
    }
}

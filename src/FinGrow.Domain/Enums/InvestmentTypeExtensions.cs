namespace FinGrow.Domain.Enums;

public static class InvestmentTypeExtensions
{
    public static PriceMarket? QuotedOn(this InvestmentType type) => type switch
    {
        InvestmentType.Stock
            or InvestmentType.Cedear
            or InvestmentType.Etf
            or InvestmentType.Bond
            or InvestmentType.CorporateBond
            or InvestmentType.TreasuryBill => PriceMarket.Exchange,
        InvestmentType.MutualFund => PriceMarket.MutualFund,
        InvestmentType.Crypto => PriceMarket.Crypto,
        _ => null,
    };

    public static bool IsQuoted(this InvestmentType type) => type.QuotedOn() is not null;

    public static bool AccruesInterest(this InvestmentType type) =>
        type is InvestmentType.FixedTermDeposit or InvestmentType.Repo or InvestmentType.RemuneratedAccount;

    public static bool HasTerm(this InvestmentType type) =>
        type is InvestmentType.FixedTermDeposit or InvestmentType.Repo;

    public static bool CompoundsDaily(this InvestmentType type) => type is InvestmentType.RemuneratedAccount;
}

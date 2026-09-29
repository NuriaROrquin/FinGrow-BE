namespace FinGrow.Domain.Enums;

public static class InvestmentTypeExtensions
{
    public static bool IsQuotedOnExchange(this InvestmentType type) =>
        type is InvestmentType.Stock or InvestmentType.Etf or InvestmentType.Bond;
}

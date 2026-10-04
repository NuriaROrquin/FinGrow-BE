namespace FinGrow.Domain.Enums;

using System.Text.RegularExpressions;
using FinGrow.Domain.Entities;

public static partial class PriceMarketExtensions
{
    public static int MaxSymbolLength(this PriceMarket market) =>
        market == PriceMarket.MutualFund ? Investment.MaxFundNameLength : Investment.MaxSymbolLength;

    public static bool IgnoresCurrency(this PriceMarket market) => market == PriceMarket.MutualFund;

    public static string NormalizeSymbol(this PriceMarket market, string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var trimmed = symbol.Trim();

        return market == PriceMarket.MutualFund
            ? RepeatedSpaces().Replace(trimmed, " ")
            : trimmed.ToUpperInvariant();
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex RepeatedSpaces();
}

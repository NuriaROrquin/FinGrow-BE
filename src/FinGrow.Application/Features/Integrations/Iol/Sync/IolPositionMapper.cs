namespace FinGrow.Application.Features.Integrations.Iol.Sync;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

internal sealed record IolHolding(
    string ExternalReference,
    string Symbol,
    string AssetName,
    InvestmentType Type,
    Currency Currency,
    decimal Quantity,
    decimal InvestedAmount,
    decimal ValuedAmount);

internal static class IolPositionMapper
{

    public static IolHolding? ToHolding(IolPosition position)
    {
        var symbol = position.Symbol.Trim();

        if (symbol.Length == 0
            || position.Quantity <= 0m
            || decimal.Round(position.Quantity * position.AveragePurchasePrice, Money.DecimalPlaces) <= 0m
            || position.ValuedAmount < 0m
            || ToInvestmentType(position.Type) is not { } type
            || ToCurrency(position.Currency) is not { } currency
            || symbol.Length > type.QuotedOn()!.Value.MaxSymbolLength())
        {
            return null;
        }

        var reference = ExternalReference(position.Market, symbol);

        if (reference.Length > Investment.MaxExternalReferenceLength)
        {
            return null;
        }

        return new IolHolding(
            reference,
            symbol,
            AssetName(symbol, position.Description),
            type,
            currency,
            position.Quantity,
            position.Quantity * position.AveragePurchasePrice,
            position.ValuedAmount);
    }


    public static IReadOnlyList<IolHolding> Consolidate(IEnumerable<IolHolding> holdings) =>
        holdings
            .GroupBy(holding => holding.ExternalReference, StringComparer.Ordinal)
            .Select(group => group.Skip(1).Aggregate(group.First(), (total, part) => total with
            {
                Quantity = total.Quantity + part.Quantity,
                InvestedAmount = total.InvestedAmount + part.InvestedAmount,
                ValuedAmount = total.ValuedAmount + part.ValuedAmount,
            }))
            .ToList();

    public static IReadOnlyDictionary<string, DateOnly> FirstPurchaseDates(IEnumerable<IolOperation> operations) =>
        operations
            .Where(operation => operation.ExecutedOn is not null && IsPurchase(operation.Type))
            .GroupBy(operation => operation.Symbol.Trim().ToUpperInvariant(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Min(operation => operation.ExecutedOn!.Value), StringComparer.Ordinal);

    public static string ExternalReference(string market, string symbol) =>
        $"{market.Trim().ToLowerInvariant()}:{symbol.Trim().ToUpperInvariant()}";

    internal static InvestmentType? ToInvestmentType(string iolType) => Normalize(iolType) switch
    {
        "acciones" => InvestmentType.Stock,
        "cedears" => InvestmentType.Cedear,
        "titulospublicos" => InvestmentType.Bond,
        "obligacionesnegociables" => InvestmentType.CorporateBond,
        "letras" => InvestmentType.TreasuryBill,
        "fondocomundeinversion" => InvestmentType.MutualFund,
        "etf" or "etfs" => InvestmentType.Etf,
        _ => null,
    };

    internal static Currency? ToCurrency(string iolCurrency) => Normalize(iolCurrency) switch
    {
        "pesoargentino" => Currency.ARS,
        "dolarestadounidense" => Currency.USD,
        _ => null,
    };

    private static string AssetName(string symbol, string? description)
    {
        var name = string.IsNullOrWhiteSpace(description) ? symbol : $"{symbol} - {description.Trim()}";

        return name.Length <= Investment.MaxAssetNameLength ? name : name[..Investment.MaxAssetNameLength].TrimEnd();
    }

    private static bool IsPurchase(string iolType)
    {
        var type = Normalize(iolType);

        return type.StartsWith("compra", StringComparison.Ordinal) || type.StartsWith("suscrip", StringComparison.Ordinal);
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetter).Select(char.ToLowerInvariant).ToArray());
}

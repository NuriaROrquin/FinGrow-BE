namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class SecurityPrice : AggregateRoot
{
    public const int MaxSourceLength = 20;

    private SecurityPrice()
    {
    }

    private SecurityPrice(Guid id, PriceMarket market, string symbol, Currency currency)
        : base(id)
    {
        Market = market;
        Symbol = symbol;
        Currency = currency;
    }

    public PriceMarket Market { get; private set; }

    public string Symbol { get; private set; } = string.Empty;

    public Currency Currency { get; private set; }

    public decimal UnitPrice { get; private set; }

    public DateOnly PricedOn { get; private set; }

    public string Source { get; private set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; private set; }

    public static SecurityPrice Create(
        PriceMarket market,
        string symbol,
        Currency currency,
        decimal unitPrice,
        DateOnly pricedOn,
        string source,
        DateTimeOffset updatedAt)
    {
        var price = new SecurityPrice(
            Guid.CreateVersion7(),
            market,
            market.NormalizeSymbol(RequiredText.Ensure(symbol, market.MaxSymbolLength(), "El simbolo")),
            currency);

        price.Update(unitPrice, pricedOn, source, updatedAt);

        return price;
    }

    public void Update(decimal unitPrice, DateOnly pricedOn, string source, DateTimeOffset updatedAt)
    {
        if (unitPrice <= 0m)
        {
            throw new DomainException("El precio tiene que ser mayor a cero.");
        }

        UnitPrice = unitPrice;
        PricedOn = pricedOn;
        Source = RequiredText.Ensure(source, MaxSourceLength, "La fuente del precio");
        UpdatedAt = updatedAt;
    }
}

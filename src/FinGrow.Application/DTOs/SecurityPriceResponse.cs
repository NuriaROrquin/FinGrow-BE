namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;
using Interfaces;

public sealed record SecurityPriceResponse(
    string Symbol,
    string? Name,
    Currency Currency,
    decimal UnitPrice,
    DateOnly PricedOn,
    string Source)
{
    public static SecurityPriceResponse FromMarketPrice(MarketPrice price, string source, DateOnly today) => new(
        price.Symbol,
        price.Name,
        price.Currency,
        price.UnitPrice,
        price.PricedOn ?? today,
        source);

    public static SecurityPriceResponse FromStored(SecurityPrice price) => new(
        price.Symbol,
        null,
        price.Currency,
        price.UnitPrice,
        price.PricedOn,
        price.Source);
}

namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record SecurityPriceResponse(
    string Symbol,
    Currency Currency,
    decimal UnitPrice,
    DateOnly PricedOn,
    string Source)
{
    public static SecurityPriceResponse FromStored(SecurityPrice price) => new(
        price.Symbol,
        price.Currency,
        price.UnitPrice,
        price.PricedOn,
        price.Source);
}

namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record InvestmentResponse(
    Guid Id,
    string AssetName,
    InvestmentType Type,
    decimal InvestedAmount,
    decimal CurrentValue,
    Currency Currency,
    decimal ReturnAmount,
    decimal ReturnPercentage,
    DateOnly PurchasedOn,
    DateOnly ValuedOn,
    DateTimeOffset CreatedAt)
{
    public static InvestmentResponse FromEntity(Investment investment) => new(
        investment.Id,
        investment.AssetName,
        investment.Type,
        investment.InvestedAmount.Amount,
        investment.CurrentValue.Amount,
        investment.InvestedAmount.Currency,
        investment.ReturnAmount,
        investment.ReturnPercentage,
        investment.PurchasedOn,
        investment.ValuedOn,
        investment.CreatedAt);
}

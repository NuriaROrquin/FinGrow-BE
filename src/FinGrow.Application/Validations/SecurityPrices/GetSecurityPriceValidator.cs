namespace FinGrow.Application.Validations.SecurityPrices;

using System.Text.RegularExpressions;
using FinGrow.Application.Features.SecurityPrices.GetSecurityPrice;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

public sealed partial class GetSecurityPriceValidator : AbstractValidator<GetSecurityPriceQuery>
{
    public GetSecurityPriceValidator()
    {
        RuleFor(query => query.Symbol)
            .Must(symbol => !string.IsNullOrWhiteSpace(symbol) && SymbolPattern().IsMatch(symbol.Trim()))
            .WithMessage($"El simbolo solo puede tener letras y numeros, hasta {Investment.MaxSymbolLength} caracteres.");

        RuleFor(query => query.Currency)
            .Must(currency => currency is Currency.ARS or Currency.USD)
            .WithMessage("BYMA cotiza en pesos o en dolares: elegi ARS o USD.");
    }

    [GeneratedRegex("^[A-Za-z0-9]{1,20}$")]
    private static partial Regex SymbolPattern();
}

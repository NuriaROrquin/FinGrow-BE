namespace FinGrow.Application.Validations.Investments;

using System.Text.RegularExpressions;
using FinGrow.Application.Features.Investments;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

internal sealed partial class InvestmentTrackingValidator : AbstractValidator<IInvestmentTracking>
{
    public InvestmentTrackingValidator()
    {
        RuleFor(tracking => tracking)
            .Must(tracking => string.IsNullOrWhiteSpace(tracking.Symbol) == (tracking.Quantity is null))
            .WithMessage("El simbolo y la cantidad van juntos: carga los dos o ninguno.");

        RuleFor(tracking => tracking.Symbol)
            .Must(symbol => SymbolPattern().IsMatch(symbol!.Trim()))
            .When(tracking => !string.IsNullOrWhiteSpace(tracking.Symbol))
            .WithMessage($"El simbolo solo puede tener letras y numeros, hasta {Investment.MaxSymbolLength} caracteres.");

        RuleFor(tracking => tracking.Symbol)
            .Must((tracking, symbol) => string.IsNullOrWhiteSpace(symbol) || tracking.Type.IsQuotedOnExchange())
            .WithMessage("Solo se cotizan por simbolo las acciones, los CEDEAR, los ETF y los bonos.");

        RuleFor(tracking => tracking.Quantity)
            .GreaterThan(0m)
            .When(tracking => tracking.Quantity.HasValue)
            .WithMessage("La cantidad tiene que ser mayor a cero.");
    }

    [GeneratedRegex("^[A-Za-z0-9]{1,20}$")]
    private static partial Regex SymbolPattern();
}

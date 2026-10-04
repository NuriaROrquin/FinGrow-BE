namespace FinGrow.Application.Validations.Investments;

using FinGrow.Application.Features.Investments;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

internal sealed class InvestmentTrackingValidator : AbstractValidator<IInvestmentTracking>
{
    public InvestmentTrackingValidator()
    {
        RuleFor(tracking => tracking)
            .Must(tracking => string.IsNullOrWhiteSpace(tracking.Symbol) == (tracking.Quantity is null))
            .WithMessage("El simbolo y la cantidad van juntos: carga los dos o ninguno.");

        RuleFor(tracking => tracking.Symbol)
            .Must((tracking, symbol) => QuoteSymbolRules.IsValid(tracking.Type.QuotedOn()!.Value, symbol))
            .When(tracking => !string.IsNullOrWhiteSpace(tracking.Symbol) && tracking.Type.IsQuoted())
            .WithMessage(tracking => QuoteSymbolRules.InvalidMessage(tracking.Type.QuotedOn()!.Value));

        RuleFor(tracking => tracking.Symbol)
            .Must((tracking, symbol) => string.IsNullOrWhiteSpace(symbol) || tracking.Type.IsQuoted())
            .WithMessage(Investment.NotQuotedMessage);

        RuleFor(tracking => tracking.Quantity)
            .GreaterThan(0m)
            .When(tracking => tracking.Quantity.HasValue)
            .WithMessage("La cantidad tiene que ser mayor a cero.");
    }
}

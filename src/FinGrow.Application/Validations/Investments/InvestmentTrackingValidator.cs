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

        RuleFor(tracking => tracking.AnnualRate)
            .NotNull()
            .When(tracking => tracking.Type.AccruesInterest())
            .WithMessage(Investment.RateRequiredMessage);

        RuleFor(tracking => tracking.AnnualRate)
            .Null()
            .When(tracking => !tracking.Type.AccruesInterest())
            .WithMessage(Investment.NotAccruingMessage);

        RuleFor(tracking => tracking.AnnualRate)
            .Must(annualRate => annualRate is > 0m and <= Investment.MaxAnnualRate)
            .When(tracking => tracking.AnnualRate.HasValue)
            .WithMessage(Investment.RateOutOfRangeMessage);

        RuleFor(tracking => tracking.AnnualRate)
            .Must(annualRate => decimal.Round(annualRate!.Value, Investment.AnnualRateDecimals) == annualRate)
            .When(tracking => tracking.AnnualRate.HasValue)
            .WithMessage(Investment.RateDecimalsMessage);

        RuleFor(tracking => tracking.TermDays)
            .NotNull()
            .When(tracking => tracking.Type.HasTerm())
            .WithMessage(Investment.TermRequiredMessage);

        RuleFor(tracking => tracking.TermDays)
            .Null()
            .When(tracking => !tracking.Type.HasTerm())
            .WithMessage(Investment.TermNotAllowedMessage);

        RuleFor(tracking => tracking.TermDays)
            .InclusiveBetween(1, Investment.MaxTermDays)
            .When(tracking => tracking.TermDays.HasValue)
            .WithMessage(Investment.TermOutOfRangeMessage);
    }
}

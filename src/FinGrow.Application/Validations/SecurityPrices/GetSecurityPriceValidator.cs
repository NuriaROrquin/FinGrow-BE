namespace FinGrow.Application.Validations.SecurityPrices;

using FinGrow.Application.Features.SecurityPrices.GetSecurityPrice;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

public sealed class GetSecurityPriceValidator : AbstractValidator<GetSecurityPriceQuery>
{
    public GetSecurityPriceValidator()
    {
        RuleFor(query => query.Type)
            .Must(type => type.IsQuoted())
            .WithMessage(Investment.NotQuotedMessage);

        When(query => query.Type.IsQuoted(), () =>
        {
            RuleFor(query => query.Symbol)
                .Must((query, symbol) => QuoteSymbolRules.IsValid(query.Type.QuotedOn()!.Value, symbol))
                .WithMessage(query => QuoteSymbolRules.InvalidMessage(query.Type.QuotedOn()!.Value));

            RuleFor(query => query.Currency)
                .Must((query, currency) => QuoteSymbolRules.IsQuotableCurrency(query.Type.QuotedOn()!.Value, currency))
                .WithMessage(query => QuoteSymbolRules.UnquotableCurrencyMessage(query.Type.QuotedOn()!.Value));
        });
    }
}

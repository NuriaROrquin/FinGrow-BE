namespace FinGrow.Application.Validations.SecurityPrices;

using FinGrow.Application.Features.SecurityPrices.SearchSecurityPrices;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

public sealed class SearchSecurityPricesValidator : AbstractValidator<SearchSecurityPricesQuery>
{
    public const int MinQueryLength = 2;

    public SearchSecurityPricesValidator()
    {
        RuleFor(query => query.Type)
            .Must(type => type.IsQuoted())
            .WithMessage(Investment.NotQuotedMessage);

        RuleFor(query => query.Query)
            .Must(text => text is not null && text.Trim().Length is >= MinQueryLength and <= Investment.MaxFundNameLength)
            .WithMessage($"Escribi entre {MinQueryLength} y {Investment.MaxFundNameLength} caracteres para buscar.");

        RuleFor(query => query.Currency)
            .IsInEnum()
            .When(query => query.Currency.HasValue);
    }
}

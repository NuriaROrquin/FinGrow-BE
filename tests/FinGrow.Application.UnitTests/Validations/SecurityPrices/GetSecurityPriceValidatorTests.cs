namespace FinGrow.Application.UnitTests.Validations.SecurityPrices;

using FinGrow.Application.Features.SecurityPrices.GetSecurityPrice;
using FinGrow.Application.Validations.SecurityPrices;
using Domain.Enums;

public class GetSecurityPriceValidatorTests
{
    private readonly GetSecurityPriceValidator _validator = new();

    [Theory]
    [InlineData("AL30", Currency.ARS)]
    [InlineData("al30d", Currency.USD)]
    [InlineData(" YPFD ", Currency.ARS)]
    public void A_symbol_in_pesos_or_dollars_passes(string symbol, Currency currency)
    {
        var result = _validator.Validate(new GetSecurityPriceQuery(symbol, currency));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AL-30")]
    [InlineData("TH08A.SB")]
    [InlineData("SIMBOLODEMASDEVEINTEX")]
    public void A_symbol_that_an_investment_would_not_accept_is_rejected(string symbol)
    {
        var result = _validator.Validate(new GetSecurityPriceQuery(symbol, Currency.ARS));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(GetSecurityPriceQuery.Symbol));
    }

    [Theory]
    [InlineData(Currency.EUR)]
    [InlineData(Currency.BRL)]
    [InlineData((Currency)0)]
    public void A_currency_that_byma_does_not_quote_is_rejected(Currency currency)
    {
        var result = _validator.Validate(new GetSecurityPriceQuery("AL30", currency));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldContain("ARS o USD");
    }
}

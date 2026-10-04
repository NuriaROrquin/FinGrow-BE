namespace FinGrow.Application.UnitTests.Validations.SecurityPrices;

using FinGrow.Application.Features.SecurityPrices.GetSecurityPrice;
using FinGrow.Application.Validations.SecurityPrices;
using Domain.Entities;
using Domain.Enums;

public class GetSecurityPriceValidatorTests
{
    private readonly GetSecurityPriceValidator _validator = new();

    [Theory]
    [InlineData("AL30", Currency.ARS, InvestmentType.Bond)]
    [InlineData("al30d", Currency.USD, InvestmentType.Bond)]
    [InlineData(" YPFD ", Currency.ARS, InvestmentType.Stock)]
    [InlineData("S30N6", Currency.ARS, InvestmentType.TreasuryBill)]
    [InlineData("btc", Currency.USD, InvestmentType.Crypto)]
    [InlineData("Balanz Capital Money Market - Clase A", Currency.ARS, InvestmentType.MutualFund)]
    [InlineData("1822 Raices Ahorro Dólares - Clase A", Currency.EUR, InvestmentType.MutualFund)]
    public void A_symbol_its_market_accepts_passes(string symbol, Currency currency, InvestmentType type)
    {
        var result = _validator.Validate(new GetSecurityPriceQuery(symbol, currency, type));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", InvestmentType.Bond)]
    [InlineData("   ", InvestmentType.Bond)]
    [InlineData("AL-30", InvestmentType.Bond)]
    [InlineData("TH08A.SB", InvestmentType.Bond)]
    [InlineData("SIMBOLODEMASDEVEINTEX", InvestmentType.Stock)]
    [InlineData("BTC-USD", InvestmentType.Crypto)]
    [InlineData("", InvestmentType.MutualFund)]
    public void A_symbol_that_an_investment_would_not_accept_is_rejected(string symbol, InvestmentType type)
    {
        var result = _validator.Validate(new GetSecurityPriceQuery(symbol, Currency.ARS, type));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(GetSecurityPriceQuery.Symbol));
    }

    [Fact]
    public void A_fund_name_longer_than_the_maximum_is_rejected()
    {
        var result = _validator.Validate(new GetSecurityPriceQuery(
            new string('F', Investment.MaxFundNameLength + 1), Currency.ARS, InvestmentType.MutualFund));

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(Currency.EUR, InvestmentType.Bond)]
    [InlineData(Currency.BRL, InvestmentType.Stock)]
    [InlineData((Currency)0, InvestmentType.Bond)]
    [InlineData(Currency.EUR, InvestmentType.Crypto)]
    public void A_currency_that_the_source_does_not_quote_is_rejected(Currency currency, InvestmentType type)
    {
        var result = _validator.Validate(new GetSecurityPriceQuery("AL30", currency, type));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldContain("ARS o USD");
    }

    [Theory]
    [InlineData(InvestmentType.FixedTermDeposit)]
    [InlineData(InvestmentType.Repo)]
    [InlineData(InvestmentType.RemuneratedAccount)]
    [InlineData((InvestmentType)0)]
    public void A_type_without_a_price_source_is_rejected(InvestmentType type)
    {
        var result = _validator.Validate(new GetSecurityPriceQuery("AL30", Currency.ARS, type));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(GetSecurityPriceQuery.Type));
    }
}

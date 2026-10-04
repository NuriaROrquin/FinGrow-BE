namespace FinGrow.Application.UnitTests.Validations.SecurityPrices;

using FinGrow.Application.Features.SecurityPrices.SearchSecurityPrices;
using FinGrow.Application.Validations.SecurityPrices;
using Domain.Enums;

public class SearchSecurityPricesValidatorTests
{
    private readonly SearchSecurityPricesValidator _validator = new();

    [Theory]
    [InlineData(InvestmentType.MutualFund, "ba")]
    [InlineData(InvestmentType.MutualFund, "Balanz Capital")]
    [InlineData(InvestmentType.Crypto, "btc")]
    [InlineData(InvestmentType.Bond, "AL3")]
    public void A_text_of_at_least_two_characters_on_a_quoted_type_passes(InvestmentType type, string query)
    {
        var result = _validator.Validate(new SearchSecurityPricesQuery(type, query));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" b ")]
    public void A_text_that_is_too_short_is_rejected(string query)
    {
        var result = _validator.Validate(new SearchSecurityPricesQuery(InvestmentType.MutualFund, query));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(SearchSecurityPricesQuery.Query));
    }

    [Fact]
    public void A_type_without_a_price_source_is_rejected()
    {
        var result = _validator.Validate(new SearchSecurityPricesQuery(InvestmentType.FixedTermDeposit, "plazo"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(SearchSecurityPricesQuery.Type));
    }
}

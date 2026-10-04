namespace FinGrow.Domain.UnitTests.Enums;

using FinGrow.Domain.Enums;

public class InvestmentTypeTests
{
    [Theory]
    [InlineData(InvestmentType.Stock, PriceMarket.Exchange)]
    [InlineData(InvestmentType.Cedear, PriceMarket.Exchange)]
    [InlineData(InvestmentType.Etf, PriceMarket.Exchange)]
    [InlineData(InvestmentType.Bond, PriceMarket.Exchange)]
    [InlineData(InvestmentType.CorporateBond, PriceMarket.Exchange)]
    [InlineData(InvestmentType.TreasuryBill, PriceMarket.Exchange)]
    [InlineData(InvestmentType.MutualFund, PriceMarket.MutualFund)]
    [InlineData(InvestmentType.Crypto, PriceMarket.Crypto)]
    public void Each_quoted_type_belongs_to_the_market_that_prices_it(InvestmentType type, PriceMarket market)
    {
        type.QuotedOn().ShouldBe(market);
        type.IsQuoted().ShouldBeTrue();
    }

    [Theory]
    [InlineData(InvestmentType.FixedTermDeposit)]
    [InlineData(InvestmentType.Repo)]
    [InlineData(InvestmentType.RemuneratedAccount)]
    public void Types_without_a_price_source_are_not_quoted(InvestmentType type)
    {
        type.QuotedOn().ShouldBeNull();
        type.IsQuoted().ShouldBeFalse();
    }

    [Fact]
    public void Only_mutual_funds_are_matched_by_name_regardless_of_currency()
    {
        PriceMarket.MutualFund.IgnoresCurrency().ShouldBeTrue();
        PriceMarket.Exchange.IgnoresCurrency().ShouldBeFalse();
        PriceMarket.Crypto.IgnoresCurrency().ShouldBeFalse();
    }
}

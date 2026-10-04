namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public class SecurityPriceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 21, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Friday = new(2026, 10, 2);

    [Fact]
    public void A_price_keeps_the_symbol_in_upper_case_with_its_currency_date_and_source()
    {
        var price = SecurityPrice.Create(" al30d ", Currency.USD, 0.5397m, Friday, "BYMA", Now);

        price.Symbol.ShouldBe("AL30D");
        price.Currency.ShouldBe(Currency.USD);
        price.UnitPrice.ShouldBe(0.5397m);
        price.PricedOn.ShouldBe(Friday);
        price.Source.ShouldBe("BYMA");
        price.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Updating_a_price_overwrites_the_close_and_keeps_the_symbol()
    {
        var price = SecurityPrice.Create("AL30", Currency.ARS, 839.40m, Friday, "BYMA", Now);

        price.Update(845m, Friday.AddDays(3), "BYMA", Now.AddDays(3));

        price.Symbol.ShouldBe("AL30");
        price.UnitPrice.ShouldBe(845m);
        price.PricedOn.ShouldBe(Friday.AddDays(3));
        price.UpdatedAt.ShouldBe(Now.AddDays(3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_price_that_is_not_positive_is_rejected(decimal unitPrice)
    {
        Should.Throw<DomainException>(() => SecurityPrice.Create("AL30", Currency.ARS, unitPrice, Friday, "BYMA", Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("SIMBOLODEMASDEVEINTEX")]
    public void A_symbol_that_an_investment_would_not_accept_is_rejected(string symbol)
    {
        Should.Throw<DomainException>(() => SecurityPrice.Create(symbol, Currency.ARS, 100m, Friday, "BYMA", Now));
    }
}

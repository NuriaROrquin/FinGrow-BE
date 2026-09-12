namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Two_equal_amounts_are_the_same_value()
    {
        Money.From(1500.50m, Currency.ARS).ShouldBe(Money.From(1500.50m, Currency.ARS));
    }

    [Fact]
    public void The_same_number_in_another_currency_is_not_the_same_value()
    {
        Money.From(100m, Currency.ARS).ShouldNotBe(Money.From(100m, Currency.USD));
    }

    [Fact]
    public void A_negative_amount_cannot_be_constructed()
    {
        Should.Throw<DomainException>(() => Money.From(-1m, Currency.ARS));
    }

    [Fact]
    public void The_amount_is_rounded_to_two_decimals()
    {
        Money.From(10.005m, Currency.ARS).Amount.ShouldBe(10.00m);
        Money.From(10.015m, Currency.ARS).Amount.ShouldBe(10.02m);
    }

    [Fact]
    public void Amounts_in_different_currencies_cannot_be_added()
    {
        var pesos = Money.From(100m, Currency.ARS);
        var dolares = Money.From(100m, Currency.USD);

        Should.Throw<DomainException>(() => pesos.Add(dolares));
    }

    [Fact]
    public void Subtracting_more_than_available_does_not_leave_a_negative_amount()
    {
        var saldo = Money.From(100m, Currency.ARS);

        Should.Throw<DomainException>(() => saldo.Subtract(Money.From(150m, Currency.ARS)));
    }

    [Fact]
    public void The_signed_difference_allows_negative_results()
    {
        Money.From(80m, Currency.USD)
            .DifferenceWith(Money.From(100m, Currency.USD))
            .ShouldBe(-20m);
    }

    [Fact]
    public void The_percentage_of_a_zero_total_is_zero_and_not_a_division_by_zero()
    {
        Money.From(50m, Currency.ARS).PercentageOf(Money.Zero(Currency.ARS)).ShouldBe(0m);
    }

    [Fact]
    public void The_percentage_is_calculated_over_the_total()
    {
        Money.From(45m, Currency.ARS).PercentageOf(Money.From(90m, Currency.ARS)).ShouldBe(50m);
    }
}

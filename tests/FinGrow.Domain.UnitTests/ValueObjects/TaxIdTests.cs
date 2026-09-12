namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class TaxIdTests
{
    // 20-12345678-6: el 6 final es el digito verificador que sale de la formula de AFIP.
    private const string ValidCuit = "20123456786";

    [Fact]
    public void A_valid_cuit_is_stored_as_eleven_digits_without_separators()
    {
        TaxId.From("20-12345678-6").Value.ShouldBe(ValidCuit);
    }

    [Fact]
    public void The_cuit_can_be_displayed_with_dashes()
    {
        TaxId.From(ValidCuit).ToDisplayString().ShouldBe("20-12345678-6");
    }

    [Fact]
    public void A_cuit_with_a_wrong_check_digit_is_rejected()
    {
        Should.Throw<DomainException>(() => TaxId.From("20123456787"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("201234567861")]
    public void A_cuit_that_does_not_have_eleven_digits_is_rejected(string candidate)
    {
        Should.Throw<DomainException>(() => TaxId.From(candidate));
    }
}

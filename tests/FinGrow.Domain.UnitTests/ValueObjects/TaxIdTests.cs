namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class TaxIdTests
{
    // 20-12345678-6: el 6 final es el digito verificador que sale de la formula de AFIP.
    private const string ValidCuit = "20123456786";

    [Fact]
    public void Un_cuit_valido_se_guarda_como_once_digitos_sin_separadores()
    {
        TaxId.From("20-12345678-6").Value.ShouldBe(ValidCuit);
    }

    [Fact]
    public void El_cuit_se_puede_mostrar_con_guiones()
    {
        TaxId.From(ValidCuit).ToDisplayString().ShouldBe("20-12345678-6");
    }

    [Fact]
    public void Un_cuit_con_digito_verificador_equivocado_se_rechaza()
    {
        Should.Throw<DomainException>(() => TaxId.From("20123456787"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("201234567861")]
    public void Un_cuit_que_no_tiene_once_digitos_se_rechaza(string candidate)
    {
        Should.Throw<DomainException>(() => TaxId.From(candidate));
    }
}

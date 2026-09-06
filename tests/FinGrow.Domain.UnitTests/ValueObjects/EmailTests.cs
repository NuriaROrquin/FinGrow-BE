namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class EmailTests
{
    [Fact]
    public void El_email_se_guarda_normalizado_en_minusculas_y_sin_espacios()
    {
        Email.From("  Juan.Perez@Empresa.COM ").Value.ShouldBe("juan.perez@empresa.com");
    }

    [Fact]
    public void Dos_emails_que_difieren_solo_en_mayusculas_son_la_misma_persona()
    {
        Email.From("ana@fingrow.com").ShouldBe(Email.From("ANA@fingrow.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sinarroba.com")]
    [InlineData("@empresa.com")]
    [InlineData("juan@")]
    [InlineData("juan@empresa")]
    [InlineData("juan@@empresa.com")]
    [InlineData("juan perez@empresa.com")]
    public void Un_email_mal_formado_se_rechaza_al_construirlo(string candidate)
    {
        Should.Throw<DomainException>(() => Email.From(candidate));
    }
}

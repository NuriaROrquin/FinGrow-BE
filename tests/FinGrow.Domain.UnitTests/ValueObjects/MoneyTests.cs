namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Dos_importes_iguales_son_el_mismo_valor()
    {
        Money.From(1500.50m, Currency.ARS).ShouldBe(Money.From(1500.50m, Currency.ARS));
    }

    [Fact]
    public void El_mismo_numero_en_otra_moneda_no_es_el_mismo_valor()
    {
        Money.From(100m, Currency.ARS).ShouldNotBe(Money.From(100m, Currency.USD));
    }

    [Fact]
    public void Un_importe_negativo_no_se_puede_construir()
    {
        Should.Throw<DomainException>(() => Money.From(-1m, Currency.ARS));
    }

    [Fact]
    public void El_importe_se_redondea_a_dos_decimales()
    {
        Money.From(10.005m, Currency.ARS).Amount.ShouldBe(10.00m);
        Money.From(10.015m, Currency.ARS).Amount.ShouldBe(10.02m);
    }

    [Fact]
    public void No_se_pueden_sumar_importes_de_monedas_distintas()
    {
        var pesos = Money.From(100m, Currency.ARS);
        var dolares = Money.From(100m, Currency.USD);

        Should.Throw<DomainException>(() => pesos.Add(dolares));
    }

    [Fact]
    public void Restar_mas_de_lo_que_hay_no_deja_un_importe_negativo()
    {
        var saldo = Money.From(100m, Currency.ARS);

        Should.Throw<DomainException>(() => saldo.Subtract(Money.From(150m, Currency.ARS)));
    }

    [Fact]
    public void La_diferencia_con_signo_admite_resultados_negativos()
    {
        Money.From(80m, Currency.USD)
            .DifferenceWith(Money.From(100m, Currency.USD))
            .ShouldBe(-20m);
    }

    [Fact]
    public void El_porcentaje_sobre_un_total_de_cero_es_cero_y_no_una_division_por_cero()
    {
        Money.From(50m, Currency.ARS).PercentageOf(Money.Zero(Currency.ARS)).ShouldBe(0m);
    }

    [Fact]
    public void El_porcentaje_se_calcula_sobre_el_total()
    {
        Money.From(45m, Currency.ARS).PercentageOf(Money.From(90m, Currency.ARS)).ShouldBe(50m);
    }
}

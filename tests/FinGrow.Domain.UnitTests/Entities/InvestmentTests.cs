namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class InvestmentTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Al_crearse_la_inversion_vale_lo_mismo_que_se_puso()
    {
        var investment = CreateInvestment();

        investment.CurrentValue.ShouldBe(investment.InvestedAmount);
        investment.ReturnAmount.ShouldBe(0m);
        investment.ReturnPercentage.ShouldBe(0m);
    }

    [Fact]
    public void El_rendimiento_positivo_se_calcula_sobre_el_capital_invertido()
    {
        var investment = CreateInvestment();

        investment.UpdateValuation(Money.From(1250m, Currency.USD), Now.AddMonths(6));

        investment.ReturnAmount.ShouldBe(250m);
        investment.ReturnPercentage.ShouldBe(25m);
    }

    [Fact]
    public void Una_inversion_puede_dar_perdida()
    {
        var investment = CreateInvestment();

        investment.UpdateValuation(Money.From(800m, Currency.USD), Now.AddMonths(6));

        investment.ReturnAmount.ShouldBe(-200m);
        investment.ReturnPercentage.ShouldBe(-20m);
    }

    [Fact]
    public void La_valuacion_tiene_que_estar_en_la_misma_moneda_que_el_capital()
    {
        var investment = CreateInvestment();

        Should.Throw<DomainException>(() =>
            investment.UpdateValuation(Money.From(1250m, Currency.ARS), Now));
    }

    [Fact]
    public void Agregar_capital_sube_el_invertido_y_el_valor_actual_sin_inventar_rendimiento()
    {
        var investment = CreateInvestment();

        investment.AddCapital(Money.From(500m, Currency.USD), Now.AddMonths(1));

        investment.InvestedAmount.Amount.ShouldBe(1500m);
        investment.CurrentValue.Amount.ShouldBe(1500m);
        investment.ReturnAmount.ShouldBe(0m);
    }

    private static Investment CreateInvestment() => Investment.Create(
        EmployeeId,
        "S&P 500 ETF",
        InvestmentType.Etf,
        Money.From(1000m, Currency.USD),
        new DateOnly(2026, 1, 10),
        Now);
}

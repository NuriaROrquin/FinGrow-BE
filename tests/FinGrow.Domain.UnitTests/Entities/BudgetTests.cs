namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class BudgetTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void El_periodo_mensual_arranca_el_primero_del_mes_aunque_se_cree_a_mitad()
    {
        var budget = CreateMonthlyBudget(50000m);

        budget.PeriodStart.ShouldBe(new DateOnly(2026, 3, 1));
        budget.PeriodEnd.ShouldBe(new DateOnly(2026, 3, 31));
    }

    [Fact]
    public void El_periodo_anual_arranca_el_primero_de_enero()
    {
        var budget = Budget.Create(
            EmployeeId,
            ExpenseCategory.Educacion,
            Money.From(600000m, Currency.ARS),
            BudgetPeriod.Yearly,
            new DateOnly(2026, 3, 15),
            Now);

        budget.PeriodStart.ShouldBe(new DateOnly(2026, 1, 1));
        budget.PeriodEnd.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Theory]
    [InlineData(0, BudgetHealth.OnTrack)]
    [InlineData(39999, BudgetHealth.OnTrack)]
    [InlineData(40000, BudgetHealth.Warning)]
    [InlineData(49999, BudgetHealth.Warning)]
    [InlineData(50000, BudgetHealth.Exceeded)]
    [InlineData(60000, BudgetHealth.Exceeded)]
    public void El_estado_del_presupuesto_sigue_los_umbrales_de_ochenta_y_cien_por_ciento(
        decimal spent,
        BudgetHealth expected)
    {
        var budget = CreateMonthlyBudget(50000m);

        budget.Evaluate(Money.From(spent, Currency.ARS)).ShouldBe(expected);
    }

    [Fact]
    public void Un_presupuesto_con_limite_cero_no_se_crea()
    {
        Should.Throw<DomainException>(() => CreateMonthlyBudget(0m));
    }

    [Fact]
    public void No_se_puede_cambiar_la_moneda_de_un_presupuesto_ya_creado()
    {
        var budget = CreateMonthlyBudget(50000m);

        Should.Throw<DomainException>(() => budget.ChangeLimit(Money.From(500m, Currency.USD), Now));
    }

    [Fact]
    public void El_presupuesto_cubre_las_fechas_de_su_periodo()
    {
        var budget = CreateMonthlyBudget(50000m);

        budget.Covers(new DateOnly(2026, 3, 31)).ShouldBeTrue();
        budget.Covers(new DateOnly(2026, 4, 1)).ShouldBeFalse();
    }

    private static Budget CreateMonthlyBudget(decimal limit) => Budget.Create(
        EmployeeId,
        ExpenseCategory.Alimentos,
        Money.From(limit, Currency.ARS),
        BudgetPeriod.Monthly,
        new DateOnly(2026, 3, 15),
        Now);
}

namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class GoalTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Deadline = new(2026, 12, 31);

    [Fact]
    public void Una_meta_nace_activa_y_en_cero()
    {
        var goal = CreateGoal();

        goal.Status.ShouldBe(GoalStatus.Active);
        goal.CurrentAmount.ShouldBe(Money.Zero(Currency.ARS));
        goal.ProgressPercentage.ShouldBe(0m);
    }

    [Fact]
    public void El_progreso_suma_los_aportes()
    {
        var goal = CreateGoal();

        goal.AddProgress(Money.From(250000m, Currency.ARS), Now);
        goal.AddProgress(Money.From(250000m, Currency.ARS), Now.AddDays(30));

        goal.CurrentAmount.Amount.ShouldBe(500000m);
        goal.ProgressPercentage.ShouldBe(50m);
        goal.RemainingAmount.Amount.ShouldBe(500000m);
    }

    [Fact]
    public void La_meta_se_marca_alcanzada_al_llegar_al_objetivo()
    {
        var goal = CreateGoal();
        var achievedAt = Now.AddDays(60);

        goal.AddProgress(Money.From(1000000m, Currency.ARS), achievedAt);

        goal.Status.ShouldBe(GoalStatus.Achieved);
        goal.AchievedAt.ShouldBe(achievedAt);
    }

    [Fact]
    public void Pasarse_del_objetivo_no_lleva_el_progreso_arriba_de_cien()
    {
        var goal = CreateGoal();

        goal.AddProgress(Money.From(1500000m, Currency.ARS), Now);

        goal.ProgressPercentage.ShouldBe(100m);
        goal.RemainingAmount.IsZero.ShouldBeTrue();
    }

    [Fact]
    public void Una_meta_ya_alcanzada_no_acepta_mas_progreso()
    {
        var goal = CreateGoal();
        goal.AddProgress(Money.From(1000000m, Currency.ARS), Now);

        Should.Throw<DomainException>(() => goal.AddProgress(Money.From(1m, Currency.ARS), Now));
    }

    [Fact]
    public void Una_meta_ya_alcanzada_no_se_cancela()
    {
        var goal = CreateGoal();
        goal.AddProgress(Money.From(1000000m, Currency.ARS), Now);

        Should.Throw<DomainException>(() => goal.Cancel(Now));
    }

    [Fact]
    public void Una_meta_con_fecha_limite_pasada_no_se_crea()
    {
        Should.Throw<DomainException>(() => Goal.Create(
            EmployeeId,
            "Viaje",
            Money.From(1000000m, Currency.ARS),
            new DateOnly(2026, 1, 1),
            Now));
    }

    [Fact]
    public void Los_dias_restantes_se_cuentan_contra_la_fecha_limite()
    {
        var goal = CreateGoal();

        goal.DaysRemaining(new DateOnly(2026, 12, 21)).ShouldBe(10);
        goal.DaysRemaining(new DateOnly(2027, 1, 10)).ShouldBe(-10);
    }

    private static Goal CreateGoal() => Goal.Create(
        EmployeeId,
        "Fondo de emergencia",
        Money.From(1000000m, Currency.ARS),
        Deadline,
        Now);
}

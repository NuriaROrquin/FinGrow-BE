namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// El tope que el empleado se pone para una categoria en un periodo.
/// </summary>
/// <remarks>
/// Lo gastado no se guarda: se calcula sumando las transacciones del periodo. Un contador
/// persistido se desincroniza en cuanto alguien edita o borra un movimiento, y despues nadie
/// sabe cual de los dos numeros es el bueno.
/// </remarks>
public sealed class Budget : AggregateRoot
{
    /// <summary>A partir de este porcentaje del limite el presupuesto pasa a estado de advertencia.</summary>
    public const decimal WarningThresholdPercentage = 80m;

    private Budget()
    {
    }

    private Budget(
        Guid id,
        Guid employeeId,
        ExpenseCategory category,
        Money limit,
        BudgetPeriod period,
        DateOnly periodStart,
        DateTimeOffset createdAt)
        : base(id)
    {
        EmployeeId = employeeId;
        Category = category;
        Limit = limit;
        Period = period;
        PeriodStart = periodStart;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid EmployeeId { get; private set; }

    public ExpenseCategory Category { get; private set; }

    public Money Limit { get; private set; } = null!;

    public BudgetPeriod Period { get; private set; }

    /// <summary>Primer dia del periodo. Se normaliza para que dos presupuestos del mismo mes colisionen.</summary>
    public DateOnly PeriodStart { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public static Budget Create(
        Guid employeeId,
        ExpenseCategory category,
        Money limit,
        BudgetPeriod period,
        DateOnly anyDayOfPeriod,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(limit);

        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Un presupuesto siempre pertenece a un empleado.");
        }

        if (limit.IsZero)
        {
            throw new DomainException("El limite de un presupuesto tiene que ser mayor a cero.");
        }

        return new Budget(
            Guid.CreateVersion7(),
            employeeId,
            category,
            limit,
            period,
            StartOfPeriod(period, anyDayOfPeriod),
            createdAt);
    }

    public void ChangeLimit(Money limit, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(limit);

        if (limit.IsZero)
        {
            throw new DomainException("El limite de un presupuesto tiene que ser mayor a cero.");
        }

        if (limit.Currency != Limit.Currency)
        {
            throw new DomainException("No se puede cambiar la moneda de un presupuesto ya creado.");
        }

        Limit = limit;
        UpdatedAt = updatedAt;
    }

    /// <summary>Ultimo dia cubierto por el presupuesto, inclusive.</summary>
    public DateOnly PeriodEnd => Period == BudgetPeriod.Monthly
        ? PeriodStart.AddMonths(1).AddDays(-1)
        : PeriodStart.AddYears(1).AddDays(-1);

    public bool Covers(DateOnly date) => date >= PeriodStart && date <= PeriodEnd;

    /// <summary>
    /// Estado del presupuesto frente a lo gastado. Los umbrales (80 % advertencia, 100 % excedido)
    /// son la regla R1 que hoy vive suelta en el frontend; queda aca para que haya una sola version.
    /// </summary>
    /// <remarks>
    /// La comparacion se hace multiplicando y no dividiendo: si primero se calculara el porcentaje
    /// y se redondeara a dos decimales, gastar 39.999 de un limite de 50.000 daria 80,00 % y
    /// dispararia una advertencia por un redondeo, no por haber llegado al umbral.
    /// </remarks>
    public BudgetHealth Evaluate(Money spent)
    {
        ArgumentNullException.ThrowIfNull(spent);

        if (spent.IsAtLeast(Limit))
        {
            return BudgetHealth.Exceeded;
        }

        return spent.Amount * 100m >= Limit.Amount * WarningThresholdPercentage
            ? BudgetHealth.Warning
            : BudgetHealth.OnTrack;
    }

    private static DateOnly StartOfPeriod(BudgetPeriod period, DateOnly date) => period == BudgetPeriod.Monthly
        ? new DateOnly(date.Year, date.Month, 1)
        : new DateOnly(date.Year, 1, 1);
}

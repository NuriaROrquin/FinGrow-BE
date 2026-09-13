namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// El plan de gastos del empleado para un periodo: un tope por categoria.
/// </summary>
/// <remarks>
/// Lo gastado no se guarda: se calcula sumando las transacciones del periodo. Un contador
/// persistido se desincroniza en cuanto alguien edita o borra un movimiento, y despues nadie
/// sabe cual de los dos numeros es el bueno.
/// </remarks>
public sealed class Budget : AggregateRoot
{
    /// <summary>A partir de este porcentaje del limite la categoria pasa a estado de advertencia.</summary>
    public const decimal WarningThresholdPercentage = 80m;

    private readonly List<BudgetCategoryLimit> _limits = new();

    private Budget()
    {
    }

    private Budget(
        Guid id,
        Guid employeeId,
        BudgetPeriod period,
        DateOnly periodStart,
        DateTimeOffset createdAt)
        : base(id)
    {
        EmployeeId = employeeId;
        Period = period;
        PeriodStart = periodStart;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid EmployeeId { get; private set; }

    public BudgetPeriod Period { get; private set; }

    /// <summary>Primer dia del periodo. Se normaliza para que dos presupuestos del mismo mes colisionen.</summary>
    public DateOnly PeriodStart { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public IReadOnlyCollection<BudgetCategoryLimit> Limits => _limits.AsReadOnly();

    public static Budget Create(
        Guid employeeId,
        BudgetPeriod period,
        DateOnly anyDayOfPeriod,
        DateTimeOffset createdAt)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Un presupuesto siempre pertenece a un empleado.");
        }

        return new Budget(
            Guid.CreateVersion7(),
            employeeId,
            period,
            StartOfPeriod(period, anyDayOfPeriod),
            createdAt);
    }

    public Currency? Currency => _limits.Count == 0 ? null : _limits[0].Limit.Currency;

    /// <summary>Ultimo dia cubierto por el presupuesto, inclusive.</summary>
    public DateOnly PeriodEnd => Period == BudgetPeriod.Monthly
        ? PeriodStart.AddMonths(1).AddDays(-1)
        : PeriodStart.AddYears(1).AddDays(-1);

    public bool Covers(DateOnly date) => date >= PeriodStart && date <= PeriodEnd;

    public Money? LimitFor(ExpenseCategory category) => FindLimit(category)?.Limit;

    public BudgetCategoryLimit SetLimit(ExpenseCategory category, Money limit, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(limit);

        if (Currency is { } currency && limit.Currency != currency)
        {
            throw new DomainException(
                $"Todos los topes de un presupuesto van en la misma moneda: este esta en {currency}.");
        }

        var existing = FindLimit(category);

        if (existing is not null)
        {
            existing.Change(limit, updatedAt);
            UpdatedAt = updatedAt;

            return existing;
        }

        var created = BudgetCategoryLimit.Create(Id, category, limit, updatedAt);
        _limits.Add(created);
        UpdatedAt = updatedAt;

        return created;
    }

    public void RemoveLimit(ExpenseCategory category, DateTimeOffset updatedAt)
    {
        var limit = FindLimit(category)
            ?? throw new DomainException("El presupuesto no tiene un tope para esa categoria.");

        _limits.Remove(limit);
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Estado de una categoria frente a lo gastado. Los umbrales (80 % advertencia, 100 % excedido)
    /// son la regla R1 que hoy vive suelta en el frontend; queda aca para que haya una sola version.
    /// </summary>
    /// <remarks>
    /// La comparacion se hace multiplicando y no dividiendo: si primero se calculara el porcentaje
    /// y se redondeara a dos decimales, gastar 39.999 de un limite de 50.000 daria 80,00 % y
    /// dispararia una advertencia por un redondeo, no por haber llegado al umbral.
    /// </remarks>
    public BudgetHealth Evaluate(ExpenseCategory category, Money spent)
    {
        ArgumentNullException.ThrowIfNull(spent);

        var limit = LimitFor(category)
            ?? throw new DomainException("El presupuesto no tiene un tope para esa categoria.");

        if (spent.IsAtLeast(limit))
        {
            return BudgetHealth.Exceeded;
        }

        return spent.Amount * 100m >= limit.Amount * WarningThresholdPercentage
            ? BudgetHealth.Warning
            : BudgetHealth.OnTrack;
    }

    public Budget Duplicate(DateOnly anyDayOfNewPeriod, DateTimeOffset createdAt)
    {
        var copy = Create(EmployeeId, Period, anyDayOfNewPeriod, createdAt);

        if (copy.PeriodStart == PeriodStart)
        {
            throw new DomainException("El presupuesto ya cubre ese periodo.");
        }

        foreach (var limit in _limits)
        {
            copy.SetLimit(limit.Category, limit.Limit, createdAt);
        }

        return copy;
    }

    private BudgetCategoryLimit? FindLimit(ExpenseCategory category) =>
        _limits.SingleOrDefault(limit => limit.Category == category);

    private static DateOnly StartOfPeriod(BudgetPeriod period, DateOnly date) => period == BudgetPeriod.Monthly
        ? new DateOnly(date.Year, date.Month, 1)
        : new DateOnly(date.Year, 1, 1);
}

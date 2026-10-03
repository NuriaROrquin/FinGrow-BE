namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public sealed class Budget : AggregateRoot
{
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

    public void ChangeCurrency(Currency currency, DateTimeOffset updatedAt)
    {
        foreach (var limit in _limits)
        {
            limit.Change(Money.From(limit.Limit.Amount, currency), updatedAt);
        }

        UpdatedAt = updatedAt;
    }

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
            copy.SetLimit(
                limit.Category,
                Money.From(limit.Limit.Amount, limit.Limit.Currency),
                createdAt);
        }

        return copy;
    }

    private BudgetCategoryLimit? FindLimit(ExpenseCategory category) =>
        _limits.SingleOrDefault(limit => limit.Category == category);

    private static DateOnly StartOfPeriod(BudgetPeriod period, DateOnly date) => period == BudgetPeriod.Monthly
        ? new DateOnly(date.Year, date.Month, 1)
        : new DateOnly(date.Year, 1, 1);
}

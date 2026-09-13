namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// El tope de una categoria dentro de un presupuesto.
/// </summary>
public sealed class BudgetCategoryLimit : Entity
{
    private BudgetCategoryLimit()
    {
    }

    private BudgetCategoryLimit(Guid id, Guid budgetId, ExpenseCategory category, Money limit, DateTimeOffset createdAt)
        : base(id)
    {
        BudgetId = budgetId;
        Category = category;
        Limit = limit;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid BudgetId { get; private set; }

    public ExpenseCategory Category { get; private set; }

    public Money Limit { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    internal static BudgetCategoryLimit Create(Guid budgetId, ExpenseCategory category, Money limit, DateTimeOffset createdAt) =>
        new(Guid.CreateVersion7(), budgetId, category, EnsureValidLimit(limit), createdAt);

    internal void Change(Money limit, DateTimeOffset updatedAt)
    {
        Limit = EnsureValidLimit(limit);
        UpdatedAt = updatedAt;
    }

    private static Money EnsureValidLimit(Money limit)
    {
        ArgumentNullException.ThrowIfNull(limit);

        return limit.IsZero
            ? throw new DomainException("El limite de una categoria tiene que ser mayor a cero.")
            : limit;
    }
}

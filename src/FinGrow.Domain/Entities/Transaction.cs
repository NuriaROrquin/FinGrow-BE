namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// Un movimiento de dinero del empleado. El importe se guarda siempre positivo; lo que dice si
/// suma o resta es <see cref="Type"/>. Hay dos fabricas y no un constructor generico para que
/// sea imposible construir un gasto con categoria de ingreso.
/// </summary>
public sealed class Transaction : AggregateRoot
{
    public const int MaxDescriptionLength = 300;
    public const int MaxExternalReferenceLength = 200;

    private Transaction()
    {
    }

    private Transaction(
        Guid id,
        Guid employeeId,
        TransactionType type,
        Money amount,
        ExpenseCategory? expenseCategory,
        IncomeCategory? incomeCategory,
        string description,
        DateOnly occurredOn,
        PaymentMethod paymentMethod,
        TransactionSource source,
        TransactionStatus status,
        string? externalReference,
        DateTimeOffset createdAt)
        : base(id)
    {
        EmployeeId = employeeId;
        Type = type;
        Amount = amount;
        ExpenseCategory = expenseCategory;
        IncomeCategory = incomeCategory;
        Description = description;
        OccurredOn = occurredOn;
        PaymentMethod = paymentMethod;
        Source = source;
        Status = status;
        ExternalReference = externalReference;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid EmployeeId { get; private set; }

    public TransactionType Type { get; private set; }

    public Money Amount { get; private set; } = null!;

    /// <summary>Solo tiene valor cuando <see cref="Type"/> es Expense.</summary>
    public ExpenseCategory? ExpenseCategory { get; private set; }

    /// <summary>Solo tiene valor cuando <see cref="Type"/> es Income.</summary>
    public IncomeCategory? IncomeCategory { get; private set; }

    public string Description { get; private set; } = string.Empty;

    /// <summary>Fecha del movimiento en la vida real, que no siempre es la de carga.</summary>
    public DateOnly OccurredOn { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public TransactionSource Source { get; private set; }

    /// <summary>
    /// Pendiente mientras espera que el empleado revise lo que propuso una integracion o la IA.
    /// Los cálculos de saldo, presupuesto y reportes solo miran los confirmados.
    /// </summary>
    public TransactionStatus Status { get; private set; }

    /// <summary>Un movimiento propuesto que todavia nadie reviso.</summary>
    public bool IsPending => Status == TransactionStatus.Pending;

    /// <summary>
    /// Id del movimiento en el sistema de origen (mail, pago, mensaje). Sirve para no volver a
    /// cargarlo cuando la integracion re-sincroniza.
    /// </summary>
    public string? ExternalReference { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public static Transaction RegisterExpense(
        Guid employeeId,
        Money amount,
        ExpenseCategory category,
        string description,
        DateOnly occurredOn,
        PaymentMethod paymentMethod,
        TransactionSource source,
        TransactionStatus status,
        DateTimeOffset createdAt,
        string? externalReference = null)
    {
        EnsureValidAmount(amount);

        if (!Enum.IsDefined(category))
        {
            throw new DomainException($"La categoria de gasto '{category}' no existe.");
        }

        return new Transaction(
            Guid.CreateVersion7(),
            EnsureValidEmployee(employeeId),
            TransactionType.Expense,
            amount,
            category,
            incomeCategory: null,
            EnsureValidDescription(description),
            occurredOn,
            paymentMethod,
            source,
            status,
            EnsureValidExternalReference(externalReference),
            createdAt);
    }

    public static Transaction RegisterIncome(
        Guid employeeId,
        Money amount,
        IncomeCategory category,
        string description,
        DateOnly occurredOn,
        PaymentMethod paymentMethod,
        TransactionSource source,
        TransactionStatus status,
        DateTimeOffset createdAt,
        string? externalReference = null)
    {
        EnsureValidAmount(amount);

        if (!Enum.IsDefined(category))
        {
            throw new DomainException($"La categoria de ingreso '{category}' no existe.");
        }

        return new Transaction(
            Guid.CreateVersion7(),
            EnsureValidEmployee(employeeId),
            TransactionType.Income,
            amount,
            expenseCategory: null,
            category,
            EnsureValidDescription(description),
            occurredOn,
            paymentMethod,
            source,
            status,
            EnsureValidExternalReference(externalReference),
            createdAt);
    }

    /// <summary>
    /// El empleado revisó la propuesta y la acepta: recién ahora el movimiento cuenta (HU-15).
    /// </summary>
    public void Confirm(DateTimeOffset confirmedAt)
    {
        if (Status == TransactionStatus.Confirmed)
        {
            throw new DomainException("El movimiento ya estaba confirmado.");
        }

        Status = TransactionStatus.Confirmed;
        UpdatedAt = confirmedAt;
    }

    public void UpdateDetails(
        Money amount,
        string description,
        DateOnly occurredOn,
        PaymentMethod paymentMethod,
        DateTimeOffset updatedAt)
    {
        EnsureValidAmount(amount);

        Amount = amount;
        Description = EnsureValidDescription(description);
        OccurredOn = occurredOn;
        PaymentMethod = paymentMethod;
        UpdatedAt = updatedAt;
    }

    /// <summary>Corregir la categoria de un gasto, por ejemplo lo que propuso la IA (HU-15).</summary>
    public void RecategorizeExpense(ExpenseCategory category, DateTimeOffset updatedAt)
    {
        if (Type != TransactionType.Expense)
        {
            throw new DomainException("Solo un gasto puede recibir una categoria de gasto.");
        }

        ExpenseCategory = category;
        UpdatedAt = updatedAt;
    }

    public void RecategorizeIncome(IncomeCategory category, DateTimeOffset updatedAt)
    {
        if (Type != TransactionType.Income)
        {
            throw new DomainException("Solo un ingreso puede recibir una categoria de ingreso.");
        }

        IncomeCategory = category;
        UpdatedAt = updatedAt;
    }

    private static Guid EnsureValidEmployee(Guid employeeId) =>
        employeeId == Guid.Empty
            ? throw new DomainException("Un movimiento siempre pertenece a un empleado.")
            : employeeId;

    private static void EnsureValidAmount(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (amount.IsZero)
        {
            throw new DomainException("El importe de un movimiento tiene que ser mayor a cero.");
        }
    }

    private static string EnsureValidDescription(string description)
    {
        var trimmed = (description ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("La descripcion del movimiento es obligatoria."),
            > MaxDescriptionLength => throw new DomainException(
                $"La descripcion no puede superar los {MaxDescriptionLength} caracteres."),
            _ => trimmed
        };
    }

    private static string? EnsureValidExternalReference(string? externalReference)
    {
        var trimmed = externalReference?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaxExternalReferenceLength
            ? throw new DomainException(
                $"La referencia externa no puede superar los {MaxExternalReferenceLength} caracteres.")
            : trimmed;
    }
}

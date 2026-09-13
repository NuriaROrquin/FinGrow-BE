namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// Una meta de ahorro: cuanto quiere juntar el empleado y para cuando.
/// </summary>
public sealed class Goal : AggregateRoot
{
    public const int MaxNameLength = 150;

    private readonly List<GoalContribution> _contributions = new();

    private Goal()
    {
    }

    private Goal(
        Guid id,
        Guid employeeId,
        string name,
        Money targetAmount,
        DateOnly deadline,
        DateTimeOffset createdAt)
        : base(id)
    {
        EmployeeId = employeeId;
        Name = name;
        TargetAmount = targetAmount;
        Deadline = deadline;
        Status = GoalStatus.Active;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid EmployeeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Money TargetAmount { get; private set; } = null!;

    public DateOnly Deadline { get; private set; }

    public GoalStatus Status { get; private set; }

    public DateTimeOffset? AchievedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public IReadOnlyCollection<GoalContribution> Contributions => _contributions.AsReadOnly();

    public static Goal Create(
        Guid employeeId,
        string name,
        Money targetAmount,
        DateOnly deadline,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(targetAmount);

        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Una meta siempre pertenece a un empleado.");
        }

        if (targetAmount.IsZero)
        {
            throw new DomainException("El monto objetivo tiene que ser mayor a cero.");
        }

        if (deadline < DateOnly.FromDateTime(createdAt.UtcDateTime))
        {
            throw new DomainException("La fecha limite de una meta no puede estar en el pasado.");
        }

        return new Goal(
            Guid.CreateVersion7(),
            employeeId,
            EnsureValidName(name),
            targetAmount,
            deadline,
            createdAt);
    }

    public Money CurrentAmount => _contributions.Aggregate(
        Money.Zero(TargetAmount.Currency),
        (total, contribution) => total.Add(contribution.Amount));

    /// <summary>Porcentaje alcanzado, tope 100 aunque el ahorro se pase del objetivo.</summary>
    public decimal ProgressPercentage => Math.Min(100m, CurrentAmount.PercentageOf(TargetAmount));

    /// <summary>Cuanto falta para llegar al objetivo. Cero si ya se alcanzo.</summary>
    public Money RemainingAmount => CurrentAmount.IsAtLeast(TargetAmount)
        ? Money.Zero(TargetAmount.Currency)
        : TargetAmount.Subtract(CurrentAmount);

    public int DaysRemaining(DateOnly today) => Deadline.DayNumber - today.DayNumber;

    /// <summary>Registra un aporte y marca la meta como alcanzada si con eso llega al objetivo.</summary>
    public GoalContribution AddContribution(
        Money amount,
        DateOnly contributedOn,
        string? note,
        DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (Status != GoalStatus.Active)
        {
            throw new DomainException("Solo se puede registrar un aporte en una meta activa.");
        }

        if (amount.Currency != TargetAmount.Currency)
        {
            throw new DomainException("El aporte tiene que estar en la misma moneda que el objetivo de la meta.");
        }

        var contribution = GoalContribution.Create(Id, amount, contributedOn, note, occurredAt);
        _contributions.Add(contribution);
        UpdatedAt = occurredAt;

        RefreshStatus(occurredAt);

        return contribution;
    }

    public void RemoveContribution(Guid contributionId, DateTimeOffset occurredAt)
    {
        if (Status == GoalStatus.Cancelled)
        {
            throw new DomainException("Una meta cancelada no se puede editar.");
        }

        var contribution = _contributions.SingleOrDefault(candidate => candidate.Id == contributionId)
            ?? throw new DomainException("El aporte no pertenece a esta meta.");

        _contributions.Remove(contribution);
        UpdatedAt = occurredAt;

        RefreshStatus(occurredAt);
    }

    public void UpdateDetails(string name, Money targetAmount, DateOnly deadline, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(targetAmount);

        if (Status == GoalStatus.Cancelled)
        {
            throw new DomainException("Una meta cancelada no se puede editar.");
        }

        if (targetAmount.Currency != TargetAmount.Currency)
        {
            throw new DomainException("No se puede cambiar la moneda de una meta ya creada.");
        }

        if (targetAmount.IsZero)
        {
            throw new DomainException("El monto objetivo tiene que ser mayor a cero.");
        }

        Name = EnsureValidName(name);
        TargetAmount = targetAmount;
        Deadline = deadline;
        UpdatedAt = updatedAt;

        RefreshStatus(updatedAt);
    }

    public void Cancel(DateTimeOffset updatedAt)
    {
        if (Status == GoalStatus.Achieved)
        {
            throw new DomainException("Una meta ya alcanzada no se cancela.");
        }

        Status = GoalStatus.Cancelled;
        UpdatedAt = updatedAt;
    }

    private void RefreshStatus(DateTimeOffset occurredAt)
    {
        if (CurrentAmount.IsAtLeast(TargetAmount))
        {
            Status = GoalStatus.Achieved;
            AchievedAt ??= occurredAt;
        }
        else
        {
            Status = GoalStatus.Active;
            AchievedAt = null;
        }
    }

    private static string EnsureValidName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("El nombre de la meta es obligatorio."),
            > MaxNameLength => throw new DomainException(
                $"El nombre de la meta no puede superar los {MaxNameLength} caracteres."),
            _ => trimmed
        };
    }
}

namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// Un aporte a una meta de ahorro.
/// </summary>
public sealed class GoalContribution : Entity
{
    public const int MaxNoteLength = 300;

    private GoalContribution()
    {
    }

    private GoalContribution(
        Guid id,
        Guid goalId,
        Money amount,
        DateOnly contributedOn,
        string? note,
        DateTimeOffset createdAt)
        : base(id)
    {
        GoalId = goalId;
        Amount = amount;
        ContributedOn = contributedOn;
        Note = note;
        CreatedAt = createdAt;
    }

    public Guid GoalId { get; private set; }

    public Money Amount { get; private set; } = null!;

    public DateOnly ContributedOn { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static GoalContribution Create(
        Guid goalId,
        Money amount,
        DateOnly contributedOn,
        string? note,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (amount.IsZero)
        {
            throw new DomainException("El aporte tiene que ser mayor a cero.");
        }

        return new GoalContribution(
            Guid.CreateVersion7(),
            goalId,
            amount,
            contributedOn,
            EnsureValidNote(note),
            createdAt);
    }

    private static string? EnsureValidNote(string? note)
    {
        var trimmed = note?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaxNoteLength
            ? throw new DomainException($"La nota de un aporte no puede superar los {MaxNoteLength} caracteres.")
            : trimmed;
    }
}

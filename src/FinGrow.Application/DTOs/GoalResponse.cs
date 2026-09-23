namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record GoalResponse(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    Currency Currency,
    decimal ProgressPercentage,
    DateOnly Deadline,
    int DaysRemaining,
    bool IsOverdue,
    GoalStatus Status,
    DateTimeOffset CreatedAt)
{
    /// <param name="today">Contra que dia se cuentan los dias restantes y se decide si vencio.</param>
    public static GoalResponse FromEntity(Goal goal, DateOnly today) => new(
        goal.Id,
        goal.Name,
        goal.TargetAmount.Amount,
        goal.CurrentAmount.Amount,
        goal.RemainingAmount.Amount,
        goal.TargetAmount.Currency,
        goal.ProgressPercentage,
        goal.Deadline,
        // Pasada la fecha limite no se muestran dias negativos: para eso esta IsOverdue.
        Math.Max(0, goal.DaysRemaining(today)),
        goal.IsOverdue(today),
        goal.Status,
        goal.CreatedAt);
}

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
    DateTimeOffset? AchievedAt,
    DateTimeOffset CreatedAt)
{
    public static GoalResponse FromEntity(Goal goal, DateOnly today) => new(
        goal.Id,
        goal.Name,
        goal.TargetAmount.Amount,
        goal.CurrentAmount.Amount,
        goal.RemainingAmount.Amount,
        goal.TargetAmount.Currency,
        goal.ProgressPercentage,
        goal.Deadline,
        Math.Max(0, goal.DaysRemaining(today)),
        goal.IsOverdue(today),
        goal.Status,
        goal.AchievedAt,
        goal.CreatedAt);
}

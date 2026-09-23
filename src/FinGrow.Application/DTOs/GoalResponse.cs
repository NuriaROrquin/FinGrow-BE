namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record GoalResponse(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    Currency Currency,
    decimal ProgressPercentage,
    DateOnly Deadline,
    GoalStatus Status,
    DateTimeOffset CreatedAt)
{
    public static GoalResponse FromEntity(Goal goal) => new(
        goal.Id,
        goal.Name,
        goal.TargetAmount.Amount,
        goal.CurrentAmount.Amount,
        goal.TargetAmount.Currency,
        goal.ProgressPercentage,
        goal.Deadline,
        goal.Status,
        goal.CreatedAt);
}

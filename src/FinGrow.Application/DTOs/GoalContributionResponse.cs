namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record GoalContributionResponse(
    Guid Id,
    Guid GoalId,
    decimal Amount,
    Currency Currency,
    DateOnly ContributedOn,
    string? Note,
    DateTimeOffset CreatedAt)
{
    public static GoalContributionResponse FromEntity(GoalContribution contribution) => new(
        contribution.Id,
        contribution.GoalId,
        contribution.Amount.Amount,
        contribution.Amount.Currency,
        contribution.ContributedOn,
        contribution.Note,
        contribution.CreatedAt);
}

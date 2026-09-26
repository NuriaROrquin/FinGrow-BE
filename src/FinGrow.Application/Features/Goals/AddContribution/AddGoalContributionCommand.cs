namespace FinGrow.Application.Features.Goals.AddContribution;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

public sealed record AddGoalContributionCommand(
    Guid EmployeeId,
    Guid GoalId,
    decimal Amount,
    Currency Currency,
    DateOnly ContributedOn,
    string? Note) : IRequest<Result<GoalContributionAddedResponse>>;

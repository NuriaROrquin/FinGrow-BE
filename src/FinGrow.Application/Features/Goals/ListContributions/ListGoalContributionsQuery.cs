namespace FinGrow.Application.Features.Goals.ListContributions;

using Common;
using DTOs;
using MediatR;

public sealed record ListGoalContributionsQuery(Guid EmployeeId, Guid GoalId)
    : IRequest<Result<IReadOnlyList<GoalContributionResponse>>>;

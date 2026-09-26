namespace FinGrow.Application.Features.Goals.RemoveContribution;

using Common;
using DTOs;
using MediatR;

public sealed record RemoveGoalContributionCommand(Guid EmployeeId, Guid GoalId, Guid ContributionId)
    : IRequest<Result<GoalResponse>>;

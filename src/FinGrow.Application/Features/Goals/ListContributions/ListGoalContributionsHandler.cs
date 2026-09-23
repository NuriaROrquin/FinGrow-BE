namespace FinGrow.Application.Features.Goals.ListContributions;

using Common;
using DTOs;
using Domain.Repositories;
using MediatR;

internal sealed class ListGoalContributionsHandler(IGoalRepository goalRepository)
    : IRequestHandler<ListGoalContributionsQuery, Result<IReadOnlyList<GoalContributionResponse>>>
{
    public async Task<Result<IReadOnlyList<GoalContributionResponse>>> Handle(
        ListGoalContributionsQuery request,
        CancellationToken cancellationToken)
    {
        var goal = await goalRepository.GetByIdAsync(request.GoalId, cancellationToken);

        if (goal is null || goal.EmployeeId != request.EmployeeId)
        {
            return Result.Failure<IReadOnlyList<GoalContributionResponse>>(GoalErrors.NotFound(request.GoalId));
        }

        IReadOnlyList<GoalContributionResponse> history = goal.Contributions
            .OrderByDescending(contribution => contribution.ContributedOn)
            .ThenByDescending(contribution => contribution.CreatedAt)
            .Select(GoalContributionResponse.FromEntity)
            .ToList();

        return Result.Success(history);
    }
}

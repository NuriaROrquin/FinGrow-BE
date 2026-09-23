namespace FinGrow.Application.Features.Goals.ListGoals;

using Common;
using DTOs;
using Domain.Repositories;
using MediatR;

internal sealed class ListGoalsHandler(IGoalRepository goalRepository)
    : IRequestHandler<ListGoalsQuery, Result<IReadOnlyList<GoalResponse>>>
{
    public async Task<Result<IReadOnlyList<GoalResponse>>> Handle(ListGoalsQuery request, CancellationToken cancellationToken)
    {
        var goals = await goalRepository.ListByEmployeeAsync(request.EmployeeId, cancellationToken);

        return Result.Success<IReadOnlyList<GoalResponse>>(goals.Select(GoalResponse.FromEntity).ToList());
    }
}

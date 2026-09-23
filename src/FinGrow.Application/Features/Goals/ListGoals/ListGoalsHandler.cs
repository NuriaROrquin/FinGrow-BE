namespace FinGrow.Application.Features.Goals.ListGoals;

using Common;
using DTOs;
using Interfaces;
using Domain.Repositories;
using MediatR;

internal sealed class ListGoalsHandler(IGoalRepository goalRepository, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ListGoalsQuery, Result<IReadOnlyList<GoalResponse>>>
{
    public async Task<Result<IReadOnlyList<GoalResponse>>> Handle(ListGoalsQuery request, CancellationToken cancellationToken)
    {
        var goals = await goalRepository.ListByEmployeeAsync(request.EmployeeId, cancellationToken);
        var today = dateTimeProvider.Today;

        return Result.Success<IReadOnlyList<GoalResponse>>(
            goals.Select(goal => GoalResponse.FromEntity(goal, today)).ToList());
    }
}

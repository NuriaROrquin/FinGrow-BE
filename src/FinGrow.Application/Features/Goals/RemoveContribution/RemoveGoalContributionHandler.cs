namespace FinGrow.Application.Features.Goals.RemoveContribution;

using Common;
using DTOs;
using Interfaces;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

internal sealed class RemoveGoalContributionHandler(
    IGoalRepository goalRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<RemoveGoalContributionCommand, Result<GoalResponse>>
{
    public async Task<Result<GoalResponse>> Handle(RemoveGoalContributionCommand request, CancellationToken cancellationToken)
    {
        var goal = await goalRepository.GetByIdAsync(request.GoalId, cancellationToken);

        if (goal is null || goal.EmployeeId != request.EmployeeId)
        {
            return Result.Failure<GoalResponse>(GoalErrors.NotFound(request.GoalId));
        }

        if (goal.Status == GoalStatus.Cancelled)
        {
            return Result.Failure<GoalResponse>(GoalErrors.Cancelled());
        }

        if (goal.Contributions.All(contribution => contribution.Id != request.ContributionId))
        {
            return Result.Failure<GoalResponse>(GoalErrors.ContributionNotFound(request.ContributionId));
        }

        goal.RemoveContribution(request.ContributionId, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GoalResponse.FromEntity(goal));
    }
}

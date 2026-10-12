namespace FinGrow.Application.Features.Goals.UpdateGoal;

using Common;
using DTOs;
using Interfaces;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

internal sealed class UpdateGoalHandler(
    IGoalRepository goalRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateGoalCommand, Result<GoalResponse>>
{
    public async Task<Result<GoalResponse>> Handle(UpdateGoalCommand request, CancellationToken cancellationToken)
    {
        var owned = Ownership.Require(
            await goalRepository.GetByIdAsync(request.GoalId, cancellationToken),
            goal => goal.EmployeeId,
            request.EmployeeId,
            GoalErrors.NotFound(request.GoalId),
            GoalErrors.NotOwned());

        if (owned.IsFailure)
        {
            return Result.Failure<GoalResponse>(owned.Error);
        }

        var goal = owned.Value;

        if (goal.Status == GoalStatus.Cancelled)
        {
            return Result.Failure<GoalResponse>(GoalErrors.NotFound(request.GoalId));
        }

        goal.UpdateDetails(
            request.Name,
            Money.From(request.TargetAmount, goal.TargetAmount.Currency),
            request.Deadline,
            dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GoalResponse.FromEntity(goal, dateTimeProvider.Today));
    }
}

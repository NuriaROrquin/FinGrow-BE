namespace FinGrow.Application.Features.Goals.DeleteGoal;

using Common;
using Interfaces;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

internal sealed class DeleteGoalHandler(
    IGoalRepository goalRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<DeleteGoalCommand, Result>
{
    public async Task<Result> Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
    {
        var owned = Ownership.Require(
            await goalRepository.GetByIdAsync(request.GoalId, cancellationToken),
            goal => goal.EmployeeId,
            request.EmployeeId,
            GoalErrors.NotFound(request.GoalId),
            GoalErrors.NotOwned());

        if (owned.IsFailure)
        {
            return Result.Failure(owned.Error);
        }

        var goal = owned.Value;

        if (goal.Status == GoalStatus.Cancelled)
        {
            return Result.Failure(GoalErrors.NotFound(request.GoalId));
        }

        goal.Cancel(dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

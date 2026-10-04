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
        var goal = await goalRepository.GetByIdAsync(request.GoalId, cancellationToken);

        if (goal is null || goal.EmployeeId != request.EmployeeId || goal.Status == GoalStatus.Cancelled)
        {
            return Result.Failure(GoalErrors.NotFound(request.GoalId));
        }

        goal.Cancel(dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

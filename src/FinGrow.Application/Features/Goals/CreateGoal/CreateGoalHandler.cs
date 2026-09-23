namespace FinGrow.Application.Features.Goals.CreateGoal;

using Common;
using DTOs;
using Interfaces;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

internal sealed class CreateGoalHandler(
    IGoalRepository goalRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateGoalCommand, Result<GoalResponse>>
{
    public async Task<Result<GoalResponse>> Handle(CreateGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = Goal.Create(
            request.EmployeeId,
            request.Name,
            Money.From(request.TargetAmount, request.Currency),
            request.Deadline,
            dateTimeProvider.UtcNow);

        goalRepository.Add(goal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GoalResponse.FromEntity(goal));
    }
}

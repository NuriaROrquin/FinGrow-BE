namespace FinGrow.Application.Features.Goals.AddContribution;

using Common;
using DTOs;
using Interfaces;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

internal sealed class AddGoalContributionHandler(
    IGoalRepository goalRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<AddGoalContributionCommand, Result<GoalContributionAddedResponse>>
{
    public async Task<Result<GoalContributionAddedResponse>> Handle(
        AddGoalContributionCommand request,
        CancellationToken cancellationToken)
    {
        var goal = await goalRepository.GetByIdAsync(request.GoalId, cancellationToken);

        if (goal is null || goal.EmployeeId != request.EmployeeId)
        {
            return Result.Failure<GoalContributionAddedResponse>(GoalErrors.NotFound(request.GoalId));
        }

        if (goal.Status != GoalStatus.Active)
        {
            return Result.Failure<GoalContributionAddedResponse>(GoalErrors.NotActive());
        }

        if (request.Currency != goal.TargetAmount.Currency)
        {
            return Result.Failure<GoalContributionAddedResponse>(GoalErrors.CurrencyMismatch());
        }

        var contribution = goal.AddContribution(
            Money.From(request.Amount, request.Currency),
            request.ContributedOn,
            request.Note,
            dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new GoalContributionAddedResponse(
            GoalResponse.FromEntity(goal, dateTimeProvider.Today),
            GoalContributionResponse.FromEntity(contribution)));
    }
}

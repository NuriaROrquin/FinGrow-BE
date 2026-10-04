namespace FinGrow.Application.Features.Budgets.DuplicatePreviousBudget;

using Common;
using DTOs;
using Interfaces;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

internal sealed class DuplicatePreviousBudgetHandler(
    IBudgetRepository budgetRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<DuplicatePreviousBudgetCommand, Result<BudgetResponse>>
{
    public async Task<Result<BudgetResponse>> Handle(
        DuplicatePreviousBudgetCommand request,
        CancellationToken cancellationToken)
    {
        var periodStart = new DateOnly(request.Year, request.Month, 1);
        var previousPeriodStart = periodStart.AddMonths(-1);

        if (await budgetRepository.ExistsForPeriodAsync(
                request.EmployeeId,
                BudgetPeriod.Monthly,
                periodStart,
                cancellationToken))
        {
            return Result.Failure<BudgetResponse>(BudgetErrors.AlreadyExists(periodStart));
        }

        var previous = await budgetRepository.FindForPeriodAsync(
            request.EmployeeId,
            BudgetPeriod.Monthly,
            previousPeriodStart,
            cancellationToken);

        if (previous is null)
        {
            return Result.Failure<BudgetResponse>(BudgetErrors.PreviousNotFound(previousPeriodStart));
        }

        var budget = previous.Duplicate(periodStart, dateTimeProvider.UtcNow);

        budgetRepository.Add(budget);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BudgetResponse.FromEntity(budget));
    }
}

namespace FinGrow.Application.Features.Budgets.RemoveCategoryLimit;

using Common;
using DTOs;
using Interfaces;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

internal sealed class RemoveBudgetCategoryLimitHandler(
    IBudgetRepository budgetRepository,
    IBudgetSpendingReadRepository spendingReadRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<RemoveBudgetCategoryLimitCommand, Result<BudgetResponse>>
{
    public async Task<Result<BudgetResponse>> Handle(
        RemoveBudgetCategoryLimitCommand request,
        CancellationToken cancellationToken)
    {
        var periodStart = new DateOnly(request.Year, request.Month, 1);

        var budget = await budgetRepository.FindForPeriodAsync(
            request.EmployeeId,
            BudgetPeriod.Monthly,
            periodStart,
            cancellationToken);

        if (budget is null)
        {
            return Result.Failure<BudgetResponse>(BudgetErrors.NotFound(periodStart));
        }

        if (budget.LimitFor(request.Category) is null)
        {
            return Result.Failure<BudgetResponse>(BudgetErrors.LimitNotFound());
        }

        if (budget.Limits.Count == 1)
        {
            return Result.Failure<BudgetResponse>(BudgetErrors.LastLimit());
        }

        budget.RemoveLimit(request.Category, dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var spentByCategory = await spendingReadRepository.GetSpentByCategoryAsync(budget, cancellationToken);

        return Result.Success(BudgetResponse.FromEntity(budget, spentByCategory));
    }
}

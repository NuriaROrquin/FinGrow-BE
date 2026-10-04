namespace FinGrow.Application.Features.Budgets.GetBudget;

using Common;
using DTOs;
using Domain.Enums;
using Domain.Repositories;
using MediatR;
using Interfaces;

internal sealed class GetBudgetHandler(
    IBudgetRepository budgetRepository,
    IBudgetSpendingReadRepository spendingReadRepository)
    : IRequestHandler<GetBudgetQuery, Result<BudgetResponse>>
{
    public async Task<Result<BudgetResponse>> Handle(GetBudgetQuery request, CancellationToken cancellationToken)
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

        var spentByCategory = await spendingReadRepository.GetSpentByCategoryAsync(budget, cancellationToken);

        return Result.Success(BudgetResponse.FromEntity(budget, spentByCategory));
    }
}

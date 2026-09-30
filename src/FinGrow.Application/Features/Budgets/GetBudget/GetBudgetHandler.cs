namespace FinGrow.Application.Features.Budgets.GetBudget;

using Common;
using DTOs;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

internal sealed class GetBudgetHandler(IBudgetRepository budgetRepository)
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

        return budget is null
            ? Result.Failure<BudgetResponse>(BudgetErrors.NotFound(periodStart))
            : Result.Success(BudgetResponse.FromEntity(budget));
    }
}

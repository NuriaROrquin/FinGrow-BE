namespace FinGrow.Application.Features.Budgets.SetCategoryLimit;

using Common;
using DTOs;
using Interfaces;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

internal sealed class SetBudgetCategoryLimitHandler(
    IBudgetRepository budgetRepository,
    IBudgetSpendingReadRepository spendingReadRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<SetBudgetCategoryLimitCommand, Result<BudgetResponse>>
{
    public async Task<Result<BudgetResponse>> Handle(
        SetBudgetCategoryLimitCommand request,
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

        var currency = budget.Currency ?? Currency.ARS;

        budget.SetLimit(request.Category, Money.From(request.Amount, currency), dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var spentByCategory = await spendingReadRepository.GetSpentByCategoryAsync(budget, cancellationToken);

        return Result.Success(BudgetResponse.FromEntity(budget, spentByCategory));
    }
}

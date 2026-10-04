namespace FinGrow.Application.Features.Budgets.ChangeCurrency;

using Common;
using DTOs;
using Interfaces;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

internal sealed class ChangeBudgetCurrencyHandler(
    IBudgetRepository budgetRepository,
    IBudgetSpendingReadRepository spendingReadRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ChangeBudgetCurrencyCommand, Result<BudgetResponse>>
{
    public async Task<Result<BudgetResponse>> Handle(
        ChangeBudgetCurrencyCommand request,
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

        if (budget.Currency != request.Currency)
        {
            budget.ChangeCurrency(request.Currency, dateTimeProvider.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var spentByCategory = await spendingReadRepository.GetSpentByCategoryAsync(budget, cancellationToken);

        return Result.Success(BudgetResponse.FromEntity(budget, spentByCategory));
    }
}

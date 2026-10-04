namespace FinGrow.Application.Features.Budgets.DeleteBudget;

using Common;
using Interfaces;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

internal sealed class DeleteBudgetHandler(
    IBudgetRepository budgetRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteBudgetCommand, Result>
{
    public async Task<Result> Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var periodStart = new DateOnly(request.Year, request.Month, 1);

        var budget = await budgetRepository.FindForPeriodAsync(
            request.EmployeeId,
            BudgetPeriod.Monthly,
            periodStart,
            cancellationToken);

        if (budget is null)
        {
            return Result.Failure(BudgetErrors.NotFound(periodStart));
        }

        budgetRepository.Remove(budget);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

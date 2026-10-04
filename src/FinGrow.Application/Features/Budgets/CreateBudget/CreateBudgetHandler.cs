namespace FinGrow.Application.Features.Budgets.CreateBudget;

using Common;
using DTOs;
using Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

internal sealed class CreateBudgetHandler(
    IBudgetRepository budgetRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateBudgetCommand, Result<BudgetResponse>>
{
    public async Task<Result<BudgetResponse>> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;
        var budget = Budget.Create(
            request.EmployeeId,
            BudgetPeriod.Monthly,
            new DateOnly(request.Year, request.Month, 1),
            now);

        if (await budgetRepository.ExistsForPeriodAsync(
                budget.EmployeeId,
                budget.Period,
                budget.PeriodStart,
                cancellationToken))
        {
            return Result.Failure<BudgetResponse>(BudgetErrors.AlreadyExists(budget.PeriodStart));
        }

        foreach (var limit in request.Limits)
        {
            budget.SetLimit(limit.Category, Money.From(limit.Amount, request.Currency), now);
        }

        budgetRepository.Add(budget);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BudgetResponse.FromEntity(budget));
    }
}

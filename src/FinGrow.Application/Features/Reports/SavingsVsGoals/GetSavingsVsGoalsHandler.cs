namespace FinGrow.Application.Features.Reports.SavingsVsGoals;

using Common;
using Interfaces;
using Domain.Common;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

internal sealed class GetSavingsVsGoalsHandler(
    IGoalRepository goalRepository,
    ITransactionReadRepository transactionReadRepository,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<GetSavingsVsGoalsQuery, Result<SavingsVsGoalsResponse>>
{
    private const int MonthsWithoutGoals = 6;

    public async Task<Result<SavingsVsGoalsResponse>> Handle(GetSavingsVsGoalsQuery request, CancellationToken cancellationToken)
    {
        var goals = (await goalRepository.ListByEmployeeAsync(request.EmployeeId, cancellationToken))
            .Where(goal => goal.TargetAmount.Currency == request.Currency && goal.Status != GoalStatus.Cancelled)
            .ToList();

        var today = dateTimeProvider.Today;
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var monthCount = request.Months ?? MonthsSinceFirstGoal(goals.Select(goal => goal.CreatedAt), currentMonth);
        var firstMonth = currentMonth.AddMonths(-(monthCount - 1));

        var totalsByMonth = (await transactionReadRepository.GetMonthlyTotalsAsync(
                request.EmployeeId,
                request.Currency,
                firstMonth,
                currentMonth.AddMonths(1).AddDays(-1),
                cancellationToken))
            .ToDictionary(totals => (totals.Year, totals.Month));

        var months = Enumerable.Range(0, monthCount)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month =>
            {
                var totals = totalsByMonth.GetValueOrDefault((month.Year, month.Month));
                var income = totals?.Income ?? 0m;
                var expense = totals?.Expense ?? 0m;
                var committed = goals
                    .Where(goal => goal.IsCommittedIn(month))
                    .Sum(goal => goal.MonthlyCommitment.Amount);
                var actualSavings = income - expense;

                return new SavingsVsGoalsMonth(
                    month.Year,
                    month.Month,
                    income,
                    expense,
                    actualSavings,
                    committed,
                    committed > 0m ? actualSavings >= committed : null);
            })
            .ToList();

        var totalIncome = months.Sum(month => month.Income);
        var totalActualSavings = months.Sum(month => month.ActualSavings);

        return Result.Success(new SavingsVsGoalsResponse(
            request.Currency,
            months,
            totalIncome,
            totalActualSavings,
            months.Sum(month => month.Committed),
            months.Count(month => month.MetTarget is not null),
            months.Count(month => month.MetTarget == true),
            totalIncome > 0m ? decimal.Round(totalActualSavings / totalIncome * 100m, 1) : null,
            goals.Count > 0));
    }

    private static int MonthsSinceFirstGoal(IEnumerable<DateTimeOffset> goalCreations, DateOnly currentMonth)
    {
        var creations = goalCreations.ToList();

        if (creations.Count == 0)
        {
            return MonthsWithoutGoals;
        }

        var first = ArgentinaTime.DateOf(creations.Min());
        var months = ((currentMonth.Year - first.Year) * 12) + currentMonth.Month - first.Month + 1;

        return Math.Clamp(months, 1, GetSavingsVsGoalsQuery.MaxMonths);
    }
}

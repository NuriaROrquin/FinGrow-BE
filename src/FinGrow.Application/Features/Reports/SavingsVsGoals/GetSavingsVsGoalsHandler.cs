namespace FinGrow.Application.Features.Reports.SavingsVsGoals;

using Common;
using Interfaces;
using Domain.Common;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

internal sealed class GetSavingsVsGoalsHandler(
    IGoalRepository goalRepository,
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

        var contributedByMonth = goals
            .SelectMany(goal => goal.Contributions)
            .GroupBy(contribution => (contribution.ContributedOn.Year, contribution.ContributedOn.Month))
            .ToDictionary(month => month.Key, month => month.Sum(contribution => contribution.Amount.Amount));

        var months = Enumerable.Range(0, monthCount)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month =>
            {
                var contributed = contributedByMonth.GetValueOrDefault((month.Year, month.Month));
                var committed = goals
                    .Where(goal => goal.IsCommittedIn(month))
                    .Sum(goal => goal.MonthlyCommitment.Amount);

                return new SavingsVsGoalsMonth(
                    month.Year,
                    month.Month,
                    contributed,
                    committed,
                    MetTarget(contributed, committed, isCurrentMonth: month == currentMonth));
            })
            .ToList();

        var totalContributed = months.Sum(month => month.Contributed);
        var totalCommitted = months.Sum(month => month.Committed);

        return Result.Success(new SavingsVsGoalsResponse(
            request.Currency,
            months,
            totalContributed,
            totalCommitted,
            months.Count(month => month.MetTarget is not null),
            months.Count(month => month.MetTarget == true),
            totalCommitted > 0m ? decimal.Round(totalContributed / totalCommitted * 100m, 1) : null,
            goals.Count > 0));
    }

    /// <summary>
    /// El mes en curso no se da por incumplido antes de que termine: cuenta solo si ya se llego a la cuota.
    /// </summary>
    private static bool? MetTarget(decimal contributed, decimal committed, bool isCurrentMonth)
    {
        if (committed <= 0m)
        {
            return null;
        }

        if (contributed >= committed)
        {
            return true;
        }

        return isCurrentMonth ? null : false;
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

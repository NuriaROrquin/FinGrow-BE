namespace FinGrow.Application.Features.Reports.FinancialInsights;

using System.Text.Json;
using Common;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using MonthlySummary;

internal sealed partial class GetFinancialInsightsHandler(
    IMonthlySummaryReadRepository monthlySummaryReadRepository,
    IAchievementRepository achievementRepository,
    IBudgetRepository budgetRepository,
    IBudgetSpendingReadRepository budgetSpendingReadRepository,
    IGoalRepository goalRepository,
    IEmployeeRepository employeeRepository,
    IAiService ai,
    IDateTimeProvider dateTimeProvider,
    ILogger<GetFinancialInsightsHandler> logger)
    : IRequestHandler<GetFinancialInsightsQuery, Result<FinancialInsightsResponse>>
{
    public const int RequiredActiveMonths = 2;

    public const int ReferenceMonths = 3;

    public const int MaxGoals = 20;

    public async Task<Result<FinancialInsightsResponse>> Handle(
        GetFinancialInsightsQuery request,
        CancellationToken cancellationToken)
    {
        var eligibility = await EvaluateEligibilityAsync(request.EmployeeId, cancellationToken);

        if (!eligibility.IsEligible)
        {
            return Result.Success(FinancialInsightsResponse.NotEligible(request, eligibility));
        }

        var periodMonths = ReportPeriod.Months(request.From, request.To);
        var referenceStart = periodMonths[0].AddMonths(-ReferenceMonths);
        var referenceEnd = periodMonths[0].AddDays(-1);

        var periodTotals = await monthlySummaryReadRepository.ListMonthlyTotalsAsync(
            request.EmployeeId,
            request.Currency,
            request.From,
            request.To,
            cancellationToken);
        var referenceTotals = await monthlySummaryReadRepository.ListMonthlyTotalsAsync(
            request.EmployeeId,
            request.Currency,
            referenceStart,
            referenceEnd,
            cancellationToken);

        var months = MonthlySummaryBuilder.Build(periodTotals, periodMonths);
        var referenceMonths = MonthlySummaryBuilder.Build(referenceTotals, ReportPeriod.Months(referenceStart, referenceEnd));
        var highlights = FinancialHighlights.From(months);
        var achievements = await ListAchievementsAsync(request, cancellationToken);

        if (!highlights.HasMovements)
        {
            return Result.Success(new FinancialInsightsResponse(
                request.Currency,
                request.From,
                request.To,
                eligibility,
                highlights,
                achievements,
                Array.Empty<FinancialRecommendation>(),
                true));
        }

        var input = new RecommendationsInput(
            await LanguageOfAsync(request.EmployeeId, cancellationToken),
            request.Currency,
            request.From,
            request.To,
            referenceMonths.Select(month => ToRecommendationMonth(month, isReference: true))
                .Concat(months.Select(month => ToRecommendationMonth(month, isReference: false)))
                .ToList(),
            await ListBudgetLimitsAsync(request, periodMonths[periodMonths.Count - 1], cancellationToken),
            await ListGoalsAsync(request, cancellationToken));

        var recommendations = await RecommendAsync(request.EmployeeId, input, cancellationToken);

        return Result.Success(new FinancialInsightsResponse(
            request.Currency,
            request.From,
            request.To,
            eligibility,
            highlights,
            achievements,
            recommendations?.Recommendations ?? Array.Empty<FinancialRecommendation>(),
            recommendations is not null));
    }

    private async Task<InsightsEligibility> EvaluateEligibilityAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var activity = await monthlySummaryReadRepository.ListMonthlyActivityAsync(employeeId, cancellationToken);
        var today = dateTimeProvider.Today;
        var currentMonth = activity.FirstOrDefault(month => month.Year == today.Year && month.Month == today.Month);
        var activeMonths = activity.Count(month => AchievementCatalog.IsActiveMonth(month.ConfirmedTransactions, month.HasIncome));

        return new InsightsEligibility(
            activeMonths >= RequiredActiveMonths,
            activeMonths,
            RequiredActiveMonths,
            currentMonth?.ConfirmedTransactions ?? 0,
            FinancialHealthFactors.MinimumConfirmedTransactions,
            currentMonth?.HasIncome ?? false);
    }

    private async Task<IReadOnlyList<AchievementResponse>> ListAchievementsAsync(
        GetFinancialInsightsQuery request,
        CancellationToken cancellationToken)
    {
        var earned = await achievementRepository.ListByEmployeeAsync(request.EmployeeId, cancellationToken);

        return earned
            .Where(achievement => ArgentinaTime.DateOf(achievement.AchievedAt) >= request.From
                && ArgentinaTime.DateOf(achievement.AchievedAt) <= request.To)
            .OrderBy(achievement => achievement.AchievedAt)
            .Select(achievement => AchievementResponse.From(AchievementCatalog.Find(achievement.Code), achievement))
            .ToList();
    }

    private async Task<Language> LanguageOfAsync(Guid employeeId, CancellationToken cancellationToken) =>
        (await employeeRepository.GetByIdAsync(employeeId, cancellationToken))?.Language ?? Language.es;

    private async Task<IReadOnlyList<RecommendationBudgetLimit>> ListBudgetLimitsAsync(
        GetFinancialInsightsQuery request,
        DateOnly lastMonth,
        CancellationToken cancellationToken)
    {
        var budget = await budgetRepository.FindForPeriodAsync(
            request.EmployeeId,
            BudgetPeriod.Monthly,
            lastMonth,
            cancellationToken);

        if (budget is null || budget.Currency != request.Currency)
        {
            return Array.Empty<RecommendationBudgetLimit>();
        }

        var spent = await budgetSpendingReadRepository.GetSpentByCategoryAsync(budget, cancellationToken);

        return budget.Limits
            .OrderBy(limit => limit.Category)
            .Select(limit => new RecommendationBudgetLimit(
                limit.Category,
                limit.Limit.Amount,
                spent.GetValueOrDefault(limit.Category)))
            .ToList();
    }

    private async Task<IReadOnlyList<RecommendationGoal>> ListGoalsAsync(
        GetFinancialInsightsQuery request,
        CancellationToken cancellationToken)
    {
        var goals = await goalRepository.ListByEmployeeAsync(request.EmployeeId, cancellationToken);

        return goals
            .Where(goal => goal.TargetAmount.Currency == request.Currency)
            .Where(goal => goal.Status == GoalStatus.Active || AchievedDuring(goal, request))
            .OrderBy(goal => goal.Deadline)
            .Take(MaxGoals)
            .Select(goal => new RecommendationGoal(
                goal.Name,
                goal.Status == GoalStatus.Achieved,
                goal.TargetAmount.Amount,
                goal.CurrentAmount.Amount,
                goal.MonthlyCommitment.Amount,
                goal.Deadline))
            .ToList();
    }

    private static bool AchievedDuring(Goal goal, GetFinancialInsightsQuery request) =>
        goal.Status == GoalStatus.Achieved
        && goal.AchievedAt is { } achievedAt
        && ArgentinaTime.DateOf(achievedAt) >= request.From
        && ArgentinaTime.DateOf(achievedAt) <= request.To;

    private static RecommendationMonth ToRecommendationMonth(MonthlySummaryMonth month, bool isReference) => new(
        month.Year,
        month.Month,
        isReference,
        month.Income,
        month.Expenses,
        month.Savings,
        month.ExpensesByCategory.ToDictionary(amount => amount.Category, amount => amount.Amount));

    private async Task<FinancialRecommendations?> RecommendAsync(
        Guid employeeId,
        RecommendationsInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ai.RecommendAsync(input, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogAiUnavailable(logger, exception, employeeId);
        }
        catch (JsonException exception)
        {
            LogAiUnavailable(logger, exception, employeeId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogAiUnavailable(logger, exception, employeeId);
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "FinGrow-AI no pudo generar recomendaciones para el empleado {EmployeeId}.")]
    private static partial void LogAiUnavailable(ILogger logger, Exception exception, Guid employeeId);
}

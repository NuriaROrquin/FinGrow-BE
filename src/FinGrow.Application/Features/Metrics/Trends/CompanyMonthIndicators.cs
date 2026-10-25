namespace FinGrow.Application.Features.Metrics.Trends;

using FinGrow.Application.Features.Metrics.GetCompanyInsights;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

internal static class CompanyMonthIndicators
{
    public static IReadOnlyDictionary<CompanyTrendIndicator, decimal> Read(
        PeriodMetrics metrics,
        IReadOnlyCollection<FinancialHealthFactors> factors,
        int completedCourses,
        MetricsAnonymizationPolicy anonymizationPolicy)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(factors);
        ArgumentNullException.ThrowIfNull(anonymizationPolicy);

        var indicators = new Dictionary<CompanyTrendIndicator, decimal>();

        if (!anonymizationPolicy.CanRevealCompany(metrics))
        {
            return indicators;
        }

        if (metrics.ParticipationRate is { } participationRate)
        {
            indicators[CompanyTrendIndicator.ParticipationRate] = participationRate;
        }

        if (anonymizationPolicy.CompanyAverageScore(metrics) is { } averageScore)
        {
            indicators[CompanyTrendIndicator.AverageFinancialHealthScore] = averageScore;
        }

        indicators[CompanyTrendIndicator.BudgetAdoption] = RateOf(metrics.EmployeesWithBudget, metrics.ActiveEmployees);
        indicators[CompanyTrendIndicator.GoalAdoption] = RateOf(metrics.EmployeesWithActiveGoal, metrics.ActiveEmployees);
        indicators[CompanyTrendIndicator.IntegrationAdoption] = RateOf(metrics.EmployeesWithIntegration, metrics.ActiveEmployees);
        indicators[CompanyTrendIndicator.GoalsAchieved] = metrics.GoalsAchieved;
        indicators[CompanyTrendIndicator.CompletedCourses] = completedCourses;

        if (!anonymizationPolicy.CanRevealCompany(factors.Count))
        {
            return indicators;
        }

        foreach (var concern in Enum.GetValues<CompanyConcern>())
        {
            if (CompanyConcernsCalculator.Measure(concern, factors) is { } measure
                && (measure.AffectedEmployees == 0 || anonymizationPolicy.CanRevealCompany(measure.AffectedEmployees)))
            {
                indicators[IndicatorOf(concern)] = measure.Percentage;
            }
        }

        return indicators;
    }

    private static decimal RateOf(int employees, int activeEmployees) =>
        decimal.Round(employees * 100m / activeEmployees, 2, MidpointRounding.ToEven);

    private static CompanyTrendIndicator IndicatorOf(CompanyConcern concern) =>
        concern switch
        {
            CompanyConcern.LowSavings => CompanyTrendIndicator.LowSavings,
            CompanyConcern.ThinCushion => CompanyTrendIndicator.ThinCushion,
            CompanyConcern.BudgetOverruns => CompanyTrendIndicator.BudgetOverruns,
            CompanyConcern.MissedGoalContributions => CompanyTrendIndicator.MissedGoalContributions,
            _ => throw new ArgumentOutOfRangeException(nameof(concern), concern, null),
        };
}

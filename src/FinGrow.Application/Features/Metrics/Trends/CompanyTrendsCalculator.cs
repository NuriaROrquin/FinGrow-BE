namespace FinGrow.Application.Features.Metrics.Trends;

internal static class CompanyTrendsCalculator
{
    public const decimal SignificantChange = 2m;

    private static readonly CompanyTrendIndicator[] LowerIsBetter =
    {
        CompanyTrendIndicator.LowSavings,
        CompanyTrendIndicator.ThinCushion,
        CompanyTrendIndicator.BudgetOverruns,
        CompanyTrendIndicator.MissedGoalContributions,
    };

    public static CompanyTrends Compare(
        IReadOnlyDictionary<CompanyTrendIndicator, decimal> previous,
        IReadOnlyDictionary<CompanyTrendIndicator, decimal> current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);

        var trends = new List<CompanyTrend>();

        foreach (var indicator in Enum.GetValues<CompanyTrendIndicator>())
        {
            if (!previous.TryGetValue(indicator, out var previousValue)
                || !current.TryGetValue(indicator, out var currentValue))
            {
                continue;
            }

            var change = decimal.Round(currentValue - previousValue, 2, MidpointRounding.ToEven);

            if (Math.Abs(change) >= SignificantChange)
            {
                trends.Add(new CompanyTrend(indicator, UnitOf(indicator), previousValue, currentValue, change));
            }
        }

        var ordered = trends
            .OrderByDescending(trend => Math.Abs(trend.Change))
            .ThenBy(trend => trend.Indicator)
            .ToList();

        return new CompanyTrends(
            ordered.Where(IsImprovement).ToList(),
            ordered.Where(trend => !IsImprovement(trend)).ToList());
    }

    public static CompanyTrendUnit UnitOf(CompanyTrendIndicator indicator) =>
        indicator switch
        {
            CompanyTrendIndicator.AverageFinancialHealthScore => CompanyTrendUnit.Points,
            CompanyTrendIndicator.GoalsAchieved or CompanyTrendIndicator.CompletedCourses => CompanyTrendUnit.Count,
            _ => CompanyTrendUnit.Percentage,
        };

    private static bool IsImprovement(CompanyTrend trend) =>
        LowerIsBetter.Contains(trend.Indicator) ? trend.Change < 0 : trend.Change > 0;
}

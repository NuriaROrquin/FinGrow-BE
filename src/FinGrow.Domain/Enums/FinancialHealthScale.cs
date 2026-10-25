namespace FinGrow.Domain.Enums;

public static class FinancialHealthScale
{
    public const int MinScore = 0;
    public const int MaxScore = 100;
    public const int FragileThreshold = 40;
    public const int StableThreshold = 60;
    public const int HealthyThreshold = 80;

    public static bool IsValid(int score) => score is >= MinScore and <= MaxScore;

    public static FinancialHealthBand BandOf(int score) =>
        score switch
        {
            >= HealthyThreshold => FinancialHealthBand.Healthy,
            >= StableThreshold => FinancialHealthBand.Stable,
            >= FragileThreshold => FinancialHealthBand.Fragile,
            _ => FinancialHealthBand.Critical
        };

    public static FinancialHealthBand BandOf(decimal score) =>
        score switch
        {
            >= HealthyThreshold => FinancialHealthBand.Healthy,
            >= StableThreshold => FinancialHealthBand.Stable,
            >= FragileThreshold => FinancialHealthBand.Fragile,
            _ => FinancialHealthBand.Critical
        };
}

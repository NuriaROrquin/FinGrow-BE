namespace FinGrow.Domain.Enums;

public static class ConfidenceScale
{
    public const double MinScore = 0d;
    public const double MaxScore = 1d;
    public const double MediumThreshold = 0.5d;
    public const double HighThreshold = 0.8d;

    public static bool IsValid(double score) => score is >= MinScore and <= MaxScore;

    public static ConfidenceLevel LevelOf(double score) =>
        score switch
        {
            >= HighThreshold => ConfidenceLevel.High,
            >= MediumThreshold => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };
}

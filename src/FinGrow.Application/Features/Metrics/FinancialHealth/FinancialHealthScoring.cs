namespace FinGrow.Application.Features.Metrics.FinancialHealth;

internal sealed class FinancialHealthScoring
{
    private FinancialHealthScoring(bool isComplete, IReadOnlyDictionary<Guid, int> scores)
    {
        IsComplete = isComplete;
        Scores = scores;
    }

    public static FinancialHealthScoring WithoutExchangeRate { get; } = new(false, new Dictionary<Guid, int>());

    public bool IsComplete { get; }

    public IReadOnlyDictionary<Guid, int> Scores { get; }

    public static FinancialHealthScoring Completed(IReadOnlyDictionary<Guid, int> scores) => new(true, scores);
}

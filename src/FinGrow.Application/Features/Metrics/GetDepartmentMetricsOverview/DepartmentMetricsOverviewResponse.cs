namespace FinGrow.Application.Features.Metrics.GetDepartmentMetricsOverview;

using FinGrow.Domain.Enums;
using System.Text.Json.Serialization;

public sealed record DepartmentMetricsOverviewResponse(
    Guid DepartmentId,
    string DepartmentName,
    DateOnly? Period,
    int? ActiveEmployees,
    int? ParticipatingEmployees,
    decimal? ParticipationRate,
    decimal? AverageFinancialHealthScore,
    FinancialHealthBand? FinancialHealthBand,
    bool NeedsAttention,
    bool IsMetricsHidden,
    int MinimumGroupSize,
    string? HiddenReason)
{
    [JsonIgnore]
    public int? Employees => ActiveEmployees;

    [JsonIgnore]
    public decimal? AverageScore => AverageFinancialHealthScore;

    [JsonIgnore]
    public decimal? Participation => ParticipationRate;

    [JsonIgnore]
    public FinancialHealthBand? Status => FinancialHealthBand;
}

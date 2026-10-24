namespace FinGrow.Application.Features.Metrics.GetDepartmentMetricsOverview;

using FinGrow.Domain.Enums;

public sealed record DepartmentMetricsOverviewResponse(
    Guid DepartmentId,
    string DepartmentName,
    DateOnly? Period,
    int? Employees,
    decimal? AverageScore,
    decimal? Participation,
    FinancialHealthBand? Status,
    bool IsMetricsHidden,
    int MinimumGroupSize,
    string? HiddenReason);

namespace FinGrow.Application.Features.Metrics.GetCompanyDepartmentMetrics;

using FinGrow.Application.Features.Metrics.GetDepartmentMetrics;

public sealed record CompanyDepartmentMetricsResponse(
    Guid DepartmentId,
    string DepartmentName,
    bool IsActive,
    IReadOnlyList<DepartmentMetricsResponse> Metrics,
    bool IsMetricsHidden,
    int MinimumGroupSize,
    string? HiddenReason);

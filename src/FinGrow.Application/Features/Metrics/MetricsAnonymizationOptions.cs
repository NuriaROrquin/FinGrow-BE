namespace FinGrow.Application.Features.Metrics;

using System.ComponentModel.DataAnnotations;

public sealed class MetricsAnonymizationOptions
{
    public const string SectionName = "Metrics";

    [Range(2, int.MaxValue, ErrorMessage = "Metrics:CompanyMinimumEmployees tiene que ser al menos 2.")]
    public int CompanyMinimumEmployees { get; init; } = 3;

    [Range(2, int.MaxValue, ErrorMessage = "Metrics:DepartmentMinimumEmployees tiene que ser al menos 2.")]
    public int DepartmentMinimumEmployees { get; init; } = 3;
}

namespace FinGrow.Application.Features.Metrics;

using FinGrow.Application.Common;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

internal sealed class MetricsAnonymizationPolicy(IOptions<MetricsAnonymizationOptions> options)
{
    public int CompanyMinimumEmployees => options.Value.CompanyMinimumEmployees;

    public int DepartmentMinimumEmployees => options.Value.DepartmentMinimumEmployees;

    public static Error NotEnoughEmployees =>
        Error.Unavailable(
            "Metrics.Anonymized",
            "No hay suficientes empleados para mostrar esta metrica de forma anonimizada.");

    public bool CanRevealCompany(PeriodMetrics metrics) =>
        metrics.ActiveEmployees >= CompanyMinimumEmployees;

    public bool CanRevealDepartment(PeriodMetrics metrics) =>
        metrics.ActiveEmployees >= DepartmentMinimumEmployees;
}

namespace FinGrow.Application.Features.Metrics.GetCompanyInsights;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// Las preocupaciones de la plantilla (HU-61) salen de una regla sobre los factores del puntaje, no
/// de la IA: un factor por debajo de <see cref="ConcernThreshold"/> es una preocupación. Con la
/// escala de <see cref="FinancialHealthFactors"/> eso es ahorrar menos del 10 % de los ingresos,
/// tener un colchón de menos de un mes y medio de gasto, pasarse en más de la mitad de los topes del
/// presupuesto o aportar menos de la mitad de la cuota de las metas. El prompt de FinGrow-AI
/// describe cada una con esas palabras: si cambia el umbral, cambian los dos.
/// </summary>
internal static class CompanyConcernsCalculator
{
    public const decimal ConcernThreshold = 50m;

    /// <summary>
    /// Cuenta, por preocupación, cuántos empleados la tienen sobre cuántos se pudo medir: presupuesto
    /// y metas solo se miden entre quienes tienen ese factor. Ordena de la que más pesa a la que menos
    /// y deja afuera las que no afectan a nadie.
    /// </summary>
    public static IReadOnlyList<CompanyConcernSummary> Count(IReadOnlyCollection<FinancialHealthFactors> factors)
    {
        ArgumentNullException.ThrowIfNull(factors);

        return new[]
            {
                Of(CompanyConcern.LowSavings, factors.Select(factor => (decimal?)factor.Savings)),
                Of(CompanyConcern.ThinCushion, factors.Select(factor => (decimal?)factor.Cushion)),
                Of(CompanyConcern.BudgetOverruns, factors.Select(factor => factor.Budget)),
                Of(CompanyConcern.MissedGoalContributions, factors.Select(factor => factor.Goals)),
            }
            .OfType<CompanyConcernSummary>()
            .OrderByDescending(concern => concern.Percentage)
            .ThenByDescending(concern => concern.AffectedEmployees)
            .ThenBy(concern => concern.Concern)
            .ToList();
    }

    private static CompanyConcernSummary? Of(CompanyConcern concern, IEnumerable<decimal?> values)
    {
        var evaluated = values.OfType<decimal>().ToList();
        var affected = evaluated.Count(value => value < ConcernThreshold);

        if (affected == 0)
        {
            return null;
        }

        var percentage = decimal.Round(affected * 100m / evaluated.Count, 2, MidpointRounding.ToEven);

        return new CompanyConcernSummary(concern, affected, evaluated.Count, percentage);
    }
}

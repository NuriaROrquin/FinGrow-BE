namespace FinGrow.Domain.Enums;

/// <summary>
/// Preocupación financiera de la plantilla (HU-61): un factor del puntaje de salud financiera que
/// le va mal a una parte de los empleados medidos. Es contrato con FinGrow-AI (<c>FinancialConcern</c>).
/// </summary>
public enum CompanyConcern
{
    LowSavings = 1,
    ThinCushion = 2,
    BudgetOverruns = 3,
    MissedGoalContributions = 4
}

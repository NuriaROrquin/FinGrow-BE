namespace FinGrow.Domain.Enums;

/// <summary>
/// Como viene un presupuesto contra su limite. No se persiste: se calcula al consultarlo.
/// </summary>
public enum BudgetHealth
{
    OnTrack = 1,
    Warning = 2,
    Exceeded = 3
}

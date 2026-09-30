namespace FinGrow.Application.Validations.Budgets;

internal static class BudgetPeriodRules
{
    // Acota el anio a algo razonable: fuera de este rango es casi seguro un error de carga.
    public const int MinYear = 2000;

    public const int MaxYear = 2100;
}

namespace FinGrow.Domain.Enums;

/// <summary>
/// Categorias de ingreso. A diferencia de <see cref="ExpenseCategory"/>, FinGrow-AI no las usa:
/// el modelo solo clasifica gastos. Aun asi se nombran en castellano para que las dos listas
/// de categorias se lean igual.
/// </summary>
public enum IncomeCategory
{
    Salario = 1,
    Freelance = 2,
    Inversiones = 3,
    Regalo = 4,
    Otros = 5
}

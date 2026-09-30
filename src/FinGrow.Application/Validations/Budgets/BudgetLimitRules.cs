namespace FinGrow.Application.Validations.Budgets;

internal static class BudgetLimitRules
{
    // numeric(18, 2) en la base: un valor mayor no entra en la columna y terminaria en un 500.
    public const decimal MaxAmount = 9_999_999_999_999_999.99m;

    public const string MustBePositiveMessage = "El tope de una categoria tiene que ser un numero mayor a cero.";

    public const string TooLargeMessage = "El tope de una categoria supera el maximo permitido.";

    public const string InvalidCategoryMessage = "La categoria no es una categoria de gasto valida.";
}

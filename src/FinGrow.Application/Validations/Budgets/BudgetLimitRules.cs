namespace FinGrow.Application.Validations.Budgets;

internal static class BudgetLimitRules
{
    public const decimal MaxAmount = 9_999_999_999_999_999.99m;

    public const string MustBePositiveMessage = "El tope de una categoria tiene que ser un numero mayor a cero.";

    public const string TooLargeMessage = "El tope de una categoria supera el maximo permitido.";

    public const string InvalidCategoryMessage = "La categoria no es una categoria de gasto valida.";
}

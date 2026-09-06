namespace FinGrow.Domain.Enums;

using System.Collections.Frozen;
using FinGrow.Domain.Errors;

/// <summary>
/// Traduccion entre el enum de C# y el texto que viaja por la red hacia y desde FinGrow-AI.
/// Ese texto es tambien lo que se guarda en PostgreSQL, para que la base y el servicio de IA
/// hablen el mismo idioma y no haya que traducir dos veces.
/// </summary>
public static class ExpenseCategoryExtensions
{
    private static readonly FrozenDictionary<ExpenseCategory, string> WireValues =
        new Dictionary<ExpenseCategory, string>
        {
            [ExpenseCategory.Alimentos] = "alimentos",
            [ExpenseCategory.Transporte] = "transporte",
            [ExpenseCategory.Vivienda] = "vivienda",
            [ExpenseCategory.Servicios] = "servicios",
            [ExpenseCategory.Salud] = "salud",
            [ExpenseCategory.Educacion] = "educacion",
            [ExpenseCategory.Entretenimiento] = "entretenimiento",
            [ExpenseCategory.Indumentaria] = "indumentaria",
            [ExpenseCategory.AhorroInversion] = "ahorro_inversion",
            [ExpenseCategory.Otros] = "otros"
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, ExpenseCategory> Categories =
        WireValues.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    public static string ToWireValue(this ExpenseCategory category) =>
        WireValues.TryGetValue(category, out var value)
            ? value
            : throw new DomainException($"La categoria de gasto '{category}' no tiene equivalente en FinGrow-AI.");

    public static ExpenseCategory FromWireValue(string value) =>
        TryFromWireValue(value, out var category)
            ? category
            : throw new DomainException($"'{value}' no es una categoria de gasto conocida.");

    public static bool TryFromWireValue(string? value, out ExpenseCategory category)
    {
        category = default;
        return value is not null && Categories.TryGetValue(value, out category);
    }
}

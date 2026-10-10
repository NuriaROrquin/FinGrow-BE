namespace FinGrow.Domain.Enums;

using System.Collections.Frozen;
using FinGrow.Domain.Errors;

public static class IncomeCategoryExtensions
{
    private static readonly FrozenDictionary<IncomeCategory, string> WireValues =
        new Dictionary<IncomeCategory, string>
        {
            [IncomeCategory.Salario] = "salario",
            [IncomeCategory.Freelance] = "freelance",
            [IncomeCategory.Inversiones] = "inversiones",
            [IncomeCategory.Regalo] = "regalo",
            [IncomeCategory.Otros] = "otros"
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, IncomeCategory> Categories =
        WireValues.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    public static string ToWireValue(this IncomeCategory category) =>
        WireValues.TryGetValue(category, out var value)
            ? value
            : throw new DomainException($"La categoria de ingreso '{category}' no tiene equivalente en FinGrow-AI.");

    public static IncomeCategory FromWireValue(string value) =>
        TryFromWireValue(value, out var category)
            ? category
            : throw new DomainException($"'{value}' no es una categoria de ingreso conocida.");

    public static bool TryFromWireValue(string? value, out IncomeCategory category)
    {
        category = default;
        return value is not null && Categories.TryGetValue(value, out category);
    }
}

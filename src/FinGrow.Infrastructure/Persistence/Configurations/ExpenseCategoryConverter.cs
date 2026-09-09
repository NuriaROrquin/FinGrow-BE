namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Guarda la categoria de gasto con el mismo texto que usa FinGrow-AI ("ahorro_inversion" y no
/// "AhorroInversion"). Un converter es la pieza de EF que traduce entre el tipo de C# y el tipo
/// de la columna: asi el dominio sigue trabajando con un enum y la base queda legible y alineada
/// con el servicio de IA.
/// </summary>
internal sealed class ExpenseCategoryConverter : ValueConverter<ExpenseCategory, string>
{
    public const int MaxLength = 32;

    public ExpenseCategoryConverter()
        : base(
            category => category.ToWireValue(),
            value => ExpenseCategoryExtensions.FromWireValue(value))
    {
    }
}

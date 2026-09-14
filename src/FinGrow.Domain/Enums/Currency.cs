namespace FinGrow.Domain.Enums;

/// <summary>
/// Monedas soportadas. Los nombres son el codigo ISO 4217 para que el valor que se guarda
/// en la base sea directamente el codigo y no haya que mapearlo.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "ReSharper",
    "InconsistentNaming",
    Justification = "Los nombres son codigos ISO 4217 y se guardan tal cual en la base.")]
public enum Currency
{
    ARS = 1,
    USD = 2,
    EUR = 3,
    BRL = 4
}

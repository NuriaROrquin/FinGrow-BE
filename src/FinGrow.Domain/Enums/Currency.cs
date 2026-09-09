namespace FinGrow.Domain.Enums;

/// <summary>
/// Monedas soportadas. Los nombres son el codigo ISO 4217 para que el valor que se guarda
/// en la base sea directamente el codigo y no haya que mapearlo.
/// </summary>
public enum Currency
{
    ARS = 1,
    USD = 2,
    EUR = 3,
    BRL = 4
}

namespace FinGrow.Domain.Enums;

/// <summary>
/// Categorias de gasto. Es un contrato compartido con FinGrow-AI: los mismos diez valores
/// viven en app/domain/enums.py. Por eso los nombres estan en castellano y no en ingles
/// como el resto de los identificadores: tienen que poder leerse uno al lado del otro.
/// </summary>
public enum ExpenseCategory
{
    Alimentos = 1,
    Transporte = 2,
    Vivienda = 3,
    Servicios = 4,
    Salud = 5,
    Educacion = 6,
    Entretenimiento = 7,
    Indumentaria = 8,
    AhorroInversion = 9,
    Otros = 10
}

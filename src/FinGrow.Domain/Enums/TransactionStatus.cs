namespace FinGrow.Domain.Enums;

/// <summary>
/// Un movimiento que entra por una integracion o por la IA no nace definitivo: nace propuesto y
/// el empleado lo revisa antes de que cuente (HU-06, HU-15). Solo los confirmados suman al saldo,
/// al presupuesto y a los reportes.
/// </summary>
public enum TransactionStatus
{
    Pending = 1,
    Confirmed = 2
}

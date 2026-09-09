namespace FinGrow.Domain.Enums;

/// <summary>
/// Por que puerta entro el movimiento. HU-18 pide poder mostrar de donde vino cada uno.
/// </summary>
public enum TransactionSource
{
    Manual = 1,
    ReceiptScan = 2,
    Gmail = 3,
    Telegram = 4,
    WhatsApp = 5,
    MercadoPago = 6
}

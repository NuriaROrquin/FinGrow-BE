namespace FinGrow.Application.Features.Receipts;

using FinGrow.Application.Common;

internal static class ReceiptErrors
{
    public static readonly Error NotOwned = Error.Forbidden(
        "Receipt.NotOwned", "El comprobante no pertenece al usuario autenticado.");

    public static readonly Error TransactionNotOwned = Error.Forbidden(
        "Transaction.NotOwned", "El movimiento no pertenece al usuario autenticado.");

    public static readonly Error ContentMismatch = Error.Validation(
        "Receipt.ContentMismatch", "El contenido del archivo no corresponde al tipo declarado.");

    public static readonly Error StorageUnavailable = Error.Unavailable(
        "Receipt.StorageUnavailable", "No pudimos acceder al almacenamiento de comprobantes. Proba de nuevo en unos minutos.");

    public static Error NotFound(Guid id) => Error.NotFound(
        "Receipt.NotFound", $"No existe un comprobante con id '{id}'.");

    public static Error FileMissing(Guid id) => Error.NotFound(
        "Receipt.FileMissing", $"El archivo del comprobante '{id}' no esta disponible.");

    public static Error TransactionNotFound(Guid id) => Error.NotFound(
        "Transaction.NotFound", $"No existe un movimiento con id '{id}'.");
}

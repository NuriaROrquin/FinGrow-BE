namespace FinGrow.Application.Features.Receipts;

using FinGrow.Application.Common;
using FinGrow.Domain.Entities;

internal static class ReceiptErrors
{
    public static readonly Error NotOwned = Error.Forbidden(
        "Receipt.NotOwned", "El comprobante no pertenece al usuario autenticado.");

    public static readonly Error ContentMismatch = Error.Validation(
        "Receipt.ContentMismatch", "El contenido del archivo no corresponde al tipo declarado.");

    public static readonly Error StorageUnavailable = Error.Unavailable(
        "Receipt.StorageUnavailable", "No pudimos acceder al almacenamiento de comprobantes. Proba de nuevo en unos minutos.");

    public static readonly Error UnsupportedType = Error.Validation(
        "Receipt.UnsupportedType",
        $"El comprobante tiene que ser {string.Join(", ", TransactionReceipt.AllowedContentTypes)}.");

    public static readonly Error InvalidSize = Error.Validation(
        "Receipt.InvalidSize",
        $"El comprobante tiene que pesar más de 0 bytes y como máximo {TransactionReceipt.MaxSizeBytes / (1024 * 1024)} MB.");

    public static readonly Error AlreadyAttached = Error.Conflict(
        "Receipt.AlreadyAttached", "Este comprobante ya está asociado a un movimiento.");

    public static readonly Error AlreadyScanned = Error.Conflict(
        "Receipt.AlreadyScanned", "Este comprobante ya se escaneó. Si descartaste la propuesta, cargalo a mano.");

    public static readonly Error Unreadable = Error.Validation(
        "Receipt.Unreadable",
        "No pudimos leer el comprobante. Probá con una foto más nítida o cargalo a mano: la imagen ya quedó guardada.");

    public static readonly Error MissingAmount = Error.Validation(
        "Receipt.MissingAmount",
        "No encontramos el total en el comprobante. Cargalo a mano: la imagen ya quedó guardada.");

    public static readonly Error UnsupportedForScan = Error.Validation(
        "Receipt.UnsupportedForScan",
        "Solo se pueden escanear fotos o PDF de una sola página. Cargalo a mano: la imagen ya quedó guardada.");

    public static readonly Error TooLargeToScan = Error.Validation(
        "Receipt.TooLargeToScan",
        "El comprobante pesa más de 5 MB y no se puede escanear. Cargalo a mano: la imagen ya quedó guardada.");

    public static readonly Error ScanUnavailable = Error.Unavailable(
        "Receipt.ScanUnavailable",
        "El escaneo no está disponible en este momento. Cargalo a mano o probá de nuevo en unos minutos.");

    public static Error NotFound(Guid id) => Error.NotFound(
        "Receipt.NotFound", $"No existe un comprobante con id '{id}'.");

    public static Error FileMissing(Guid id) => Error.NotFound(
        "Receipt.FileMissing", $"El archivo del comprobante '{id}' no esta disponible.");
}

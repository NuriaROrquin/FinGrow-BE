namespace FinGrow.Application.Features.Transactions;

using FinGrow.Application.Common;

internal static class TransactionReviewErrors
{
    public static readonly Error Unauthenticated = Error.Forbidden(
        "Transactions.Unauthenticated", "Hay que iniciar sesion para revisar un movimiento propuesto.");

    public static readonly Error NotOwned = Error.Forbidden(
        "Transaction.NotOwned", "El movimiento no pertenece al usuario autenticado.");

    public static readonly Error NotPending = Error.Conflict(
        "Transaction.NotPending", "El movimiento ya fue revisado: solo se puede confirmar o descartar un pendiente.");

    public static Error NotFound(Guid id) => Error.NotFound(
        "Transaction.NotFound", $"No existe un movimiento con id '{id}'.");
}

namespace FinGrow.Application.Features.Receipts;

using FinGrow.Application.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;

internal static class ReceiptAttachment
{
    public static async Task<Result<TransactionReceipt>> FindOwnedAsync(
        ITransactionReceiptRepository receipts,
        Guid receiptId,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var receipt = await receipts.GetByIdAsync(receiptId, cancellationToken);

        if (receipt is null)
        {
            return Result.Failure<TransactionReceipt>(ReceiptErrors.NotFound(receiptId));
        }

        return receipt.EmployeeId == employeeId
            ? Result.Success(receipt)
            : Result.Failure<TransactionReceipt>(ReceiptErrors.NotOwned);
    }

    public static Guid? ReceiptOf(this IReadOnlyDictionary<Guid, Guid> receiptIds, Guid transactionId) =>
        receiptIds.TryGetValue(transactionId, out var receiptId) ? receiptId : null;

    // Un movimiento tiene un solo comprobante: el que tenía queda suelto.
    public static async Task AttachAsync(
        ITransactionReceiptRepository receipts,
        TransactionReceipt receipt,
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        var current = await receipts.GetByTransactionIdAsync(transaction.Id, cancellationToken);

        if (current is not null && current.Id != receipt.Id)
        {
            current.Detach();
        }

        receipt.AttachTo(transaction);
    }
}

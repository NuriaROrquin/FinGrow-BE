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

        return Ownership.Require(
            receipt,
            found => found.EmployeeId,
            employeeId,
            ReceiptErrors.NotFound(receiptId),
            ReceiptErrors.NotOwned);
    }

    public static Guid? ReceiptOf(this IReadOnlyDictionary<Guid, Guid> receiptIds, Guid transactionId) =>
        receiptIds.TryGetValue(transactionId, out var receiptId) ? receiptId : null;

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

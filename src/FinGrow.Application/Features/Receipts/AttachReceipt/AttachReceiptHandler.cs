namespace FinGrow.Application.Features.Receipts.AttachReceipt;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class AttachReceiptHandler(
    ITransactionReceiptRepository receipts,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<AttachReceiptCommand, Result<TransactionReceiptResponse>>
{
    public async Task<Result<TransactionReceiptResponse>> Handle(
        AttachReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = currentUser.UserId!.Value;

        var found = await ReceiptAttachment.FindOwnedAsync(receipts, request.ReceiptId, employeeId, cancellationToken);

        if (found.IsFailure)
        {
            return Result.Failure<TransactionReceiptResponse>(found.Error);
        }

        var receipt = found.Value;

        var transaction = await transactions.GetByIdAsync(request.TransactionId, cancellationToken);

        if (transaction is null)
        {
            return Result.Failure<TransactionReceiptResponse>(ReceiptErrors.TransactionNotFound(request.TransactionId));
        }

        if (transaction.EmployeeId != employeeId)
        {
            return Result.Failure<TransactionReceiptResponse>(ReceiptErrors.TransactionNotOwned);
        }

        await ReceiptAttachment.AttachAsync(receipts, receipt, transaction, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TransactionReceiptResponse.FromEntity(receipt));
    }
}

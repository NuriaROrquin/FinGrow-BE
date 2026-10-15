namespace FinGrow.Application.Features.Receipts.UploadReceipt;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Receipts.Storing;
using FinGrow.Application.Interfaces;
using MediatR;

internal sealed class UploadReceiptHandler(ReceiptStorer storer, ICurrentUser currentUser)
    : IRequestHandler<UploadReceiptCommand, Result<TransactionReceiptResponse>>
{
    public async Task<Result<TransactionReceiptResponse>> Handle(
        UploadReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var stored = await storer.StoreAsync(
            currentUser.UserId!.Value,
            request.Content,
            request.ContentType,
            request.FileName,
            cancellationToken);

        return stored.IsSuccess
            ? Result.Success(TransactionReceiptResponse.FromEntity(stored.Value.Receipt))
            : Result.Failure<TransactionReceiptResponse>(stored.Error);
    }
}

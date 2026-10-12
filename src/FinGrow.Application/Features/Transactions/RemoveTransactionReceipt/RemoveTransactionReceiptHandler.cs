namespace FinGrow.Application.Features.Transactions.RemoveTransactionReceipt;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class RemoveTransactionReceiptHandler(
    ITransactionRepository transactionRepository,
    ITransactionReceiptRepository receiptRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<RemoveTransactionReceiptCommand, Result>
{
    public async Task<Result> Handle(RemoveTransactionReceiptCommand request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.UserId!.Value;

        var owned = Ownership.Require(
            await transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken),
            transaction => transaction.EmployeeId,
            employeeId,
            TransactionReviewErrors.NotFound(request.TransactionId),
            TransactionReviewErrors.NotOwned);

        if (owned.IsFailure)
        {
            return Result.Failure(owned.Error);
        }

        var transaction = owned.Value;

        var receipt = await receiptRepository.GetByTransactionIdAsync(transaction.Id, cancellationToken);

        if (receipt is null)
        {
            return Result.Success();
        }

        receipt.Detach();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

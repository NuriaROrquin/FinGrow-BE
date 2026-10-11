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

        var transaction = await transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);

        if (transaction is null || transaction.EmployeeId != employeeId)
        {
            return Result.Failure(
                Error.NotFound("Transaction.NotFound", $"No existe un movimiento con id '{request.TransactionId}'."));
        }

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

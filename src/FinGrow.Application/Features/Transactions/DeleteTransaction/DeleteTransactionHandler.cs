namespace FinGrow.Application.Features.Transactions.DeleteTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class DeleteTransactionHandler(
    ITransactionRepository transactionRepository,
    ITransactionReceiptRepository receiptRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<DeleteTransactionCommand, Result>
{
    public async Task<Result> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure(
                Error.Forbidden("Transactions.Unauthenticated", "Hay que iniciar sesion para eliminar un movimiento."));
        }

        var owned = Ownership.Require(
            await transactionRepository.GetByIdAsync(request.Id, cancellationToken),
            transaction => transaction.EmployeeId,
            employeeId,
            TransactionReviewErrors.NotFound(request.Id),
            TransactionReviewErrors.NotOwned);

        if (owned.IsFailure)
        {
            return Result.Failure(owned.Error);
        }

        var transaction = owned.Value;

        transaction.Eliminate(dateTimeProvider.UtcNow);
        (await receiptRepository.GetByTransactionIdAsync(transaction.Id, cancellationToken))?.Detach();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

namespace FinGrow.Application.Features.Transactions.DiscardTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class DiscardTransactionHandler(
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<DiscardTransactionCommand, Result>
{
    public async Task<Result> Handle(DiscardTransactionCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure(TransactionReviewErrors.Unauthenticated);
        }

        var transaction = await transactionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (transaction is null)
        {
            return Result.Failure(TransactionReviewErrors.NotFound(request.Id));
        }

        if (transaction.EmployeeId != employeeId)
        {
            return Result.Failure(TransactionReviewErrors.NotOwned);
        }

        if (!transaction.IsPending)
        {
            return Result.Failure(TransactionReviewErrors.NotPending);
        }

        transaction.Discard(dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

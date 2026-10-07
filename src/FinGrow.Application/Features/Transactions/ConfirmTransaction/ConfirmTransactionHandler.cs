namespace FinGrow.Application.Features.Transactions.ConfirmTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class ConfirmTransactionHandler(
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ConfirmTransactionCommand, Result<TransactionResponse>>
{
    public async Task<Result<TransactionResponse>> Handle(
        ConfirmTransactionCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<TransactionResponse>(TransactionReviewErrors.Unauthenticated);
        }

        var transaction = await transactionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (transaction is null)
        {
            return Result.Failure<TransactionResponse>(TransactionReviewErrors.NotFound(request.Id));
        }

        if (transaction.EmployeeId != employeeId)
        {
            return Result.Failure<TransactionResponse>(TransactionReviewErrors.NotOwned);
        }

        if (!transaction.IsPending)
        {
            return Result.Failure<TransactionResponse>(TransactionReviewErrors.NotPending);
        }

        transaction.Correct(
            request.Type,
            Money.From(request.Amount, request.Currency),
            request.ExpenseCategory,
            request.IncomeCategory,
            request.Description,
            request.OccurredOn,
            request.PaymentMethod,
            TransactionStatus.Confirmed,
            dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TransactionResponse.FromEntity(transaction));
    }
}

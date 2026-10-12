namespace FinGrow.Application.Features.Transactions.UpdateTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Receipts;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class UpdateTransactionHandler(
    ITransactionRepository transactionRepository,
    ITransactionReceiptRepository receiptRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateTransactionCommand, Result<TransactionResponse>>
{
    public async Task<Result<TransactionResponse>> Handle(
        UpdateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<TransactionResponse>(
                Error.Forbidden("Transactions.Unauthenticated", "Hay que iniciar sesion para editar un movimiento."));
        }

        var owned = Ownership.Require(
            await transactionRepository.GetByIdAsync(request.Id, cancellationToken),
            transaction => transaction.EmployeeId,
            employeeId,
            TransactionReviewErrors.NotFound(request.Id),
            TransactionReviewErrors.NotOwned);

        if (owned.IsFailure)
        {
            return Result.Failure<TransactionResponse>(owned.Error);
        }

        var transaction = owned.Value;

        TransactionReceipt? newReceipt = null;

        if (request.ReceiptId is { } receiptId)
        {
            var found = await ReceiptAttachment.FindOwnedAsync(receiptRepository, receiptId, employeeId, cancellationToken);

            if (found.IsFailure)
            {
                return Result.Failure<TransactionResponse>(found.Error);
            }

            newReceipt = found.Value;
        }

        transaction.Correct(
            request.Type,
            Money.From(request.Amount, request.Currency),
            request.ExpenseCategory,
            request.IncomeCategory,
            request.Description,
            request.OccurredOn,
            request.PaymentMethod,
            request.Status,
            dateTimeProvider.UtcNow);

        if (newReceipt is not null)
        {
            await ReceiptAttachment.AttachAsync(receiptRepository, newReceipt, transaction, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var currentReceiptId = newReceipt?.Id
            ?? (await receiptRepository.GetByTransactionIdAsync(transaction.Id, cancellationToken))?.Id;

        return Result.Success(TransactionResponse.FromEntity(transaction, currentReceiptId));
    }
}
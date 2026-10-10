namespace FinGrow.Application.Features.Transactions.CreateTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Receipts;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class CreateTransactionHandler(
    ITransactionRepository transactionRepository,
    ITransactionReceiptRepository receiptRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateTransactionCommand, Result<TransactionResponse>>
{

    public async Task<Result<TransactionResponse>> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        TransactionReceipt? receipt = null;

        if (request.ReceiptId is { } receiptId)
        {
            receipt = await receiptRepository.GetByIdAsync(receiptId, cancellationToken);

            if (receipt is null)
            {
                return Result.Failure<TransactionResponse>(ReceiptErrors.NotFound(receiptId));
            }

            if (receipt.EmployeeId != request.EmployeeId)
            {
                return Result.Failure<TransactionResponse>(ReceiptErrors.NotOwned);
            }
        }

        var amount = Money.From(request.Amount, request.Currency);
        var createdAt = dateTimeProvider.UtcNow;

        var transaction = request.Type == TransactionType.Expense
            ? Transaction.RegisterExpense(
                request.EmployeeId,
                amount,
                request.ExpenseCategory!.Value,
                request.Description,
                request.OccurredOn,
                request.PaymentMethod,
                TransactionSource.Manual,
                TransactionStatus.Confirmed,
                createdAt)
            : Transaction.RegisterIncome(
                request.EmployeeId,
                amount,
                request.IncomeCategory!.Value,
                request.Description,
                request.OccurredOn,
                request.PaymentMethod,
                TransactionSource.Manual,
                TransactionStatus.Confirmed,
                createdAt);

        transactionRepository.Add(transaction);
        receipt?.AttachTo(transaction);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TransactionResponse.FromEntity(transaction));
    }
}

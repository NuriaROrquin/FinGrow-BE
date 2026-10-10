namespace FinGrow.Application.Features.Transactions.ListPendingTransactions;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Receipts;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListPendingTransactionsHandler(
    ITransactionRepository transactionRepository,
    ITransactionReceiptRepository receiptRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListPendingTransactionsQuery, Result<PendingTransactionsResponse>>
{
    public async Task<Result<PendingTransactionsResponse>> Handle(
        ListPendingTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<PendingTransactionsResponse>(TransactionReviewErrors.Unauthenticated);
        }

        var pending = await transactionRepository.ListPendingByEmployeeAsync(employeeId, cancellationToken);
        var receiptIds = await receiptRepository.GetIdsByTransactionAsync(
            pending.Select(transaction => transaction.Id).ToList(), cancellationToken);
        var items = pending
            .Select(transaction => TransactionResponse.FromEntity(transaction, receiptIds.ReceiptOf(transaction.Id)))
            .ToList();

        return Result.Success(new PendingTransactionsResponse(items, items.Count));
    }
}

namespace FinGrow.Application.Features.Transactions.ListPendingTransactions;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListPendingTransactionsHandler(
    ITransactionRepository transactionRepository,
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
        var items = pending.Select(TransactionResponse.FromEntity).ToList();

        return Result.Success(new PendingTransactionsResponse(items, items.Count));
    }
}

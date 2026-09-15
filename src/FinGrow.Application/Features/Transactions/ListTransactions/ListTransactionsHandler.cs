namespace FinGrow.Application.Features.Transactions.ListTransactions;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs.Transactions;
using FinGrow.Domain.Services.Transactions;
using MediatR;

internal class ListTransactionsHandler(ITransactionService transactionService)
    : IRequestHandler<ListTransactionsRequest, Result<IReadOnlyList<TransactionResponse>>>
{
    public async Task<Result<IReadOnlyList<TransactionResponse>>> Handle(
        ListTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        var transactions = await transactionService.ListByEmployeeAsync(request.EmployeeId, cancellationToken);

        IReadOnlyList<TransactionResponse> response = transactions
            .Select(TransactionResponse.FromEntity)
            .ToList();

        return Result.Success(response);
    }
}

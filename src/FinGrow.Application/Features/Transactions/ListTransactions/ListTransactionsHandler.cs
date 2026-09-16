namespace FinGrow.Application.Features.Transactions.ListTransactions;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListTransactionsHandler(ITransactionRepository transactionRepository)
    : IRequestHandler<ListTransactionsCommand, Result<IReadOnlyList<TransactionResponse>>>
{
    public async Task<Result<IReadOnlyList<TransactionResponse>>> Handle(
        ListTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.ListByEmployeeAsync(request.EmployeeId, cancellationToken);

        IReadOnlyList<TransactionResponse> response = transactions
            .Select(TransactionResponse.FromEntity)
            .ToList();

        return Result.Success(response);
    }
}

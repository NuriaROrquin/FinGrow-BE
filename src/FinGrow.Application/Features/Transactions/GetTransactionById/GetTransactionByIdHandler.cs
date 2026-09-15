namespace FinGrow.Application.Features.Transactions.GetTransactionById;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs.Transactions;
using FinGrow.Domain.Services.Transactions;
using MediatR;

internal class GetTransactionByIdHandler(ITransactionService transactionService)
    : IRequestHandler<GetTransactionByIdRequest, Result<TransactionResponse>>
{
    public async Task<Result<TransactionResponse>> Handle(GetTransactionByIdRequest request, CancellationToken cancellationToken)
    {
        var transaction = await transactionService.GetByIdAsync(request.Id, cancellationToken);

        return transaction is null
            ? Result.Failure<TransactionResponse>(
                Error.NotFound("Transaction.NotFound", $"No existe un movimiento con id '{request.Id}'."))
            : Result.Success(TransactionResponse.FromEntity(transaction));
    }
}

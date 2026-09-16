namespace FinGrow.Application.Features.Transactions.GetTransactionById;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetTransactionByIdHandler(ITransactionRepository transactionRepository)
    : IRequestHandler<GetTransactionByIdCommand, Result<TransactionResponse>>
{
    public async Task<Result<TransactionResponse>> Handle(GetTransactionByIdCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(request.Id, cancellationToken);

        return transaction is null
            ? Result.Failure<TransactionResponse>(
                Error.NotFound("Transaction.NotFound", $"No existe un movimiento con id '{request.Id}'."))
            : Result.Success(TransactionResponse.FromEntity(transaction));
    }
}

namespace FinGrow.Application.Features.Transactions.GetTransactionById;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetTransactionByIdHandler(
    ITransactionRepository transactionRepository,
    ITransactionReceiptRepository receiptRepository)
    : IRequestHandler<GetTransactionByIdCommand, Result<TransactionResponse>>
{
    public async Task<Result<TransactionResponse>> Handle(GetTransactionByIdCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (transaction is null)
        {
            return Result.Failure<TransactionResponse>(
                Error.NotFound("Transaction.NotFound", $"No existe un movimiento con id '{request.Id}'."));
        }

        var receipt = await receiptRepository.GetByTransactionIdAsync(transaction.Id, cancellationToken);

        return Result.Success(TransactionResponse.FromEntity(transaction, receipt?.Id));
    }
}

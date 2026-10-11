namespace FinGrow.Application.Features.Transactions.GetTransactionById;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetTransactionByIdHandler(
    ITransactionRepository transactionRepository,
    ITransactionReceiptRepository receiptRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetTransactionByIdCommand, Result<TransactionResponse>>
{
    public async Task<Result<TransactionResponse>> Handle(GetTransactionByIdCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<TransactionResponse>(
                Error.Forbidden("Transactions.Unauthenticated", "Hay que iniciar sesion para consultar un movimiento."));
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

        var receipt = await receiptRepository.GetByTransactionIdAsync(transaction.Id, cancellationToken);

        return Result.Success(TransactionResponse.FromEntity(transaction, receipt?.Id));
    }
}

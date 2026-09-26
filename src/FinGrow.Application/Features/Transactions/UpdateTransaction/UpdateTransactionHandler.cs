namespace FinGrow.Application.Features.Transactions.UpdateTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class UpdateTransactionHandler(
    ITransactionRepository transactionRepository,
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

        var transaction = await transactionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (transaction is null || transaction.EmployeeId != employeeId)
        {
            return Result.Failure<TransactionResponse>(
                Error.NotFound("Transaction.NotFound", $"No existe un movimiento con id '{request.Id}'."));
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TransactionResponse.FromEntity(transaction));
    }
}
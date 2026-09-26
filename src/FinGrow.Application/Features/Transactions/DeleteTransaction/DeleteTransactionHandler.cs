namespace FinGrow.Application.Features.Transactions.DeleteTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class DeleteTransactionHandler(
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<DeleteTransactionCommand, Result>
{
    public async Task<Result> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure(
                Error.Forbidden("Transactions.Unauthenticated", "Hay que iniciar sesion para eliminar un movimiento."));
        }

        var transaction = await transactionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (transaction is null || transaction.EmployeeId != employeeId)
        {
            return Result.Failure(
                Error.NotFound("Transaction.NotFound", $"No existe un movimiento con id '{request.Id}'."));
        }

        transaction.Eliminate(dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

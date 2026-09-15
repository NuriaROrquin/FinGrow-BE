namespace FinGrow.Application.Features.Transactions.CreateTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs.Transactions;
using FinGrow.Domain.Services.Transactions;
using MediatR;

internal class CreateTransactionHandler(ITransactionService transactionService)
    : IRequestHandler<CreateTransactionRequest, Result<TransactionResponse>>
{
    public async Task<Result<TransactionResponse>> Handle(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var dto = request.RequestDto;

        var input = new CreateTransactionInput(
            request.EmployeeId,
            dto.Type,
            dto.Amount,
            dto.Currency,
            dto.ExpenseCategory,
            dto.IncomeCategory,
            dto.Description,
            dto.OccurredOn,
            dto.PaymentMethod);

        var transaction = await transactionService.CreateAsync(input, cancellationToken);

        return Result.Success(TransactionResponse.FromEntity(transaction));
    }
}

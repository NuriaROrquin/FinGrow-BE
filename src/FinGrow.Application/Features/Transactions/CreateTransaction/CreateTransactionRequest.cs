namespace FinGrow.Application.Features.Transactions.CreateTransaction;

using Common;
using FinGrow.Application.DTOs.Transactions;
using MediatR;

public record CreateTransactionRequest(
    Guid EmployeeId,
    CreateTransactionRequestDto RequestDto) : IRequest<Result<TransactionResponse>>;

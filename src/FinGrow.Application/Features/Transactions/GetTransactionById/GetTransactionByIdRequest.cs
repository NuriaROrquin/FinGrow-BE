namespace FinGrow.Application.Features.Transactions.GetTransactionById;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs.Transactions;
using MediatR;

public record GetTransactionByIdRequest(Guid Id) : IRequest<Result<TransactionResponse>>;

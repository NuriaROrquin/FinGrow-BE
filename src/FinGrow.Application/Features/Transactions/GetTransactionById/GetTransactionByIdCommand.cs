namespace FinGrow.Application.Features.Transactions.GetTransactionById;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record GetTransactionByIdCommand(Guid Id) : IRequest<Result<TransactionResponse>>;

namespace FinGrow.Application.Features.Transactions.DeleteTransaction;

using FinGrow.Application.Common;
using MediatR;

public sealed record DeleteTransactionCommand(Guid Id) : IRequest<Result>;

namespace FinGrow.Application.Features.Transactions.DiscardTransaction;

using FinGrow.Application.Common;
using MediatR;

public sealed record DiscardTransactionCommand(Guid Id) : IRequest<Result>;

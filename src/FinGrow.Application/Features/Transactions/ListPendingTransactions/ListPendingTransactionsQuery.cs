namespace FinGrow.Application.Features.Transactions.ListPendingTransactions;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ListPendingTransactionsQuery : IRequest<Result<PendingTransactionsResponse>>;

public sealed record PendingTransactionsResponse(IReadOnlyList<TransactionResponse> Items, int TotalCount);

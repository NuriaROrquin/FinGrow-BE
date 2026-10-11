namespace FinGrow.Application.Features.Transactions.RemoveTransactionReceipt;

using FinGrow.Application.Common;
using MediatR;

public sealed record RemoveTransactionReceiptCommand(Guid TransactionId) : IRequest<Result>;

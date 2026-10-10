namespace FinGrow.Application.Features.Receipts.AttachReceipt;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record AttachReceiptCommand(Guid ReceiptId, Guid TransactionId) : IRequest<Result<TransactionReceiptResponse>>;

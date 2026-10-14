namespace FinGrow.Application.Features.Receipts.ScanReceipt;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ScanReceiptCommand(Guid ReceiptId) : IRequest<Result<TransactionResponse>>;

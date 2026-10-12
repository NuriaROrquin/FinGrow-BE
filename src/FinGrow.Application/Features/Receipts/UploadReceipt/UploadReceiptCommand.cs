namespace FinGrow.Application.Features.Receipts.UploadReceipt;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record UploadReceiptCommand(Stream Content, string ContentType, long Length, string? FileName)
    : IRequest<Result<TransactionReceiptResponse>>;

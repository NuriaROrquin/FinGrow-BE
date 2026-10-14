namespace FinGrow.Application.Features.Receipts.ScanReceipt;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Receipts.Scanning;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class ScanReceiptHandler(
    ITransactionReceiptRepository receipts,
    ITransactionRepository transactions,
    IFileStorage storage,
    ReceiptScanner scanner,
    ICurrentUser currentUser,
    ILogger<ScanReceiptHandler> logger)
    : IRequestHandler<ScanReceiptCommand, Result<TransactionResponse>>
{
    public async Task<Result<TransactionResponse>> Handle(ScanReceiptCommand request, CancellationToken cancellationToken)
    {
        var owned = await ReceiptAttachment.FindOwnedAsync(
            receipts, request.ReceiptId, currentUser.UserId!.Value, cancellationToken);

        if (owned.IsFailure)
        {
            return Result.Failure<TransactionResponse>(owned.Error);
        }

        var receipt = owned.Value;

        if (receipt.TransactionId is { } transactionId)
        {
            return await CurrentProposalAsync(receipt, transactionId, cancellationToken);
        }

        var content = await ReadAsync(receipt, cancellationToken);

        if (content.IsFailure)
        {
            return Result.Failure<TransactionResponse>(content.Error);
        }

        var proposal = await scanner.ProposeAsync(
            receipt, content.Value, ReceiptScanner.ExternalReferenceOf(receipt), cancellationToken);

        return proposal is { Outcome: ScanOutcome.Proposed, Transaction: { } transaction }
            ? Result.Success(TransactionResponse.FromEntity(transaction, receipt.Id))
            : Result.Failure<TransactionResponse>(ErrorOf(proposal.Outcome));
    }

    private async Task<Result<TransactionResponse>> CurrentProposalAsync(
        TransactionReceipt receipt,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await transactions.GetByIdAsync(transactionId, cancellationToken);

        return transaction is { IsPending: true, Source: TransactionSource.ReceiptScan }
            ? Result.Success(TransactionResponse.FromEntity(transaction, receipt.Id))
            : Result.Failure<TransactionResponse>(ReceiptErrors.AlreadyAttached);
    }

    private async Task<Result<ReadOnlyMemory<byte>>> ReadAsync(
        TransactionReceipt receipt,
        CancellationToken cancellationToken)
    {
        StoredFile? file;

        try
        {
            file = await storage.OpenAsync(receipt.StorageKey, cancellationToken);
        }
        catch (FileStorageUnavailableException exception)
        {
            LogStorageUnavailable(logger, exception, receipt.Id);
            return Result.Failure<ReadOnlyMemory<byte>>(ReceiptErrors.StorageUnavailable);
        }

        if (file is null)
        {
            LogFileMissing(logger, receipt.Id, receipt.StorageKey);
            return Result.Failure<ReadOnlyMemory<byte>>(ReceiptErrors.FileMissing(receipt.Id));
        }

        await using var content = file.Content;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        return Result.Success<ReadOnlyMemory<byte>>(buffer.ToArray());
    }

    private static Error ErrorOf(ScanOutcome outcome) =>
        outcome switch
        {
            ScanOutcome.AlreadyProposed => ReceiptErrors.AlreadyScanned,
            ScanOutcome.Unreadable => ReceiptErrors.Unreadable,
            ScanOutcome.MissingAmount => ReceiptErrors.MissingAmount,
            ScanOutcome.UnsupportedDocument => ReceiptErrors.UnsupportedForScan,
            ScanOutcome.TooLarge => ReceiptErrors.TooLargeToScan,
            _ => ReceiptErrors.ScanUnavailable
        };

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo leer el comprobante {ReceiptId} para escanearlo: el almacenamiento no responde.")]
    private static partial void LogStorageUnavailable(ILogger logger, Exception exception, Guid receiptId);

    [LoggerMessage(Level = LogLevel.Error, Message = "El comprobante {ReceiptId} figura en la base pero no hay archivo en {StorageKey}.")]
    private static partial void LogFileMissing(ILogger logger, Guid receiptId, string storageKey);
}

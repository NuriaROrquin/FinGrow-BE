namespace FinGrow.Application.Features.Receipts.UploadReceipt;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class UploadReceiptHandler(
    ITransactionReceiptRepository receipts,
    IFileStorage storage,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<UploadReceiptHandler> logger)
    : IRequestHandler<UploadReceiptCommand, Result<TransactionReceiptResponse>>
{
    public async Task<Result<TransactionReceiptResponse>> Handle(
        UploadReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = currentUser.UserId!.Value;

        using var buffer = new MemoryStream();
        await CopyAtMostAsync(request.Content, buffer, TransactionReceipt.MaxSizeBytes + 1, cancellationToken);

        if (buffer.Length is 0 or > TransactionReceipt.MaxSizeBytes)
        {
            return Result.Failure<TransactionReceiptResponse>(Error.Validation(
                "Receipt.InvalidSize",
                $"El comprobante tiene que pesar más de 0 bytes y como máximo {TransactionReceipt.MaxSizeBytes / (1024 * 1024)} MB."));
        }

        if (!TransactionReceipt.MatchesSignature(request.ContentType, buffer.GetBuffer().AsSpan(0, (int)buffer.Length)))
        {
            return Result.Failure<TransactionReceiptResponse>(ReceiptErrors.ContentMismatch);
        }

        var receipt = TransactionReceipt.Upload(
            employeeId, request.ContentType, buffer.Length, request.FileName, clock.UtcNow);

        buffer.Position = 0;

        try
        {
            await storage.SaveAsync(receipt.StorageKey, buffer, receipt.ContentType, cancellationToken);
        }
        catch (FileStorageUnavailableException exception)
        {
            LogStorageUnavailable(logger, exception, employeeId);
            return Result.Failure<TransactionReceiptResponse>(ReceiptErrors.StorageUnavailable);
        }

        receipts.Add(receipt);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DeleteOrphanAsync(receipt.StorageKey);
            throw;
        }

        return Result.Success(TransactionReceiptResponse.FromEntity(receipt));
    }

    private static async Task CopyAtMostAsync(Stream source, Stream destination, long limit, CancellationToken cancellationToken)
    {
        var chunk = new byte[81920];
        long copied = 0;
        int read;

        while (copied < limit
               && (read = await source.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, limit - copied)), cancellationToken)) > 0)
        {
            await destination.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            copied += read;
        }
    }

    private async Task DeleteOrphanAsync(string storageKey)
    {
        try
        {
            await storage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (FileStorageUnavailableException exception)
        {
            LogOrphanNotDeleted(logger, exception, storageKey);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo guardar el comprobante del empleado {EmployeeId}: el almacenamiento no responde.")]
    private static partial void LogStorageUnavailable(ILogger logger, Exception exception, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Quedo un archivo sin comprobante en {StorageKey}: fallo la base y no se pudo borrar.")]
    private static partial void LogOrphanNotDeleted(ILogger logger, Exception exception, string storageKey);
}

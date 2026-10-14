namespace FinGrow.Application.Features.Receipts.Storing;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.Logging;

internal sealed partial class ReceiptStorer(
    ITransactionReceiptRepository receipts,
    IFileStorage storage,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<ReceiptStorer> logger)
{
    public async Task<Result<StoredReceipt>> StoreAsync(
        Guid employeeId,
        Stream content,
        string contentType,
        string? fileName,
        CancellationToken cancellationToken)
    {
        if (!TransactionReceipt.IsSupportedContentType(contentType))
        {
            return Result.Failure<StoredReceipt>(ReceiptErrors.UnsupportedType);
        }

        using var buffer = new MemoryStream();
        await CopyAtMostAsync(content, buffer, TransactionReceipt.MaxSizeBytes + 1, cancellationToken);

        if (buffer.Length is 0 or > TransactionReceipt.MaxSizeBytes)
        {
            return Result.Failure<StoredReceipt>(ReceiptErrors.InvalidSize);
        }

        var bytes = buffer.ToArray();

        if (!TransactionReceipt.MatchesSignature(contentType, bytes))
        {
            return Result.Failure<StoredReceipt>(ReceiptErrors.ContentMismatch);
        }

        var receipt = TransactionReceipt.Upload(employeeId, contentType, bytes.Length, fileName, clock.UtcNow);

        try
        {
            using var stored = new MemoryStream(bytes, writable: false);
            await storage.SaveAsync(receipt.StorageKey, stored, receipt.ContentType, cancellationToken);
        }
        catch (FileStorageUnavailableException exception)
        {
            LogStorageUnavailable(logger, exception, employeeId);
            return Result.Failure<StoredReceipt>(ReceiptErrors.StorageUnavailable);
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

        return Result.Success(new StoredReceipt(receipt, bytes));
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

internal sealed record StoredReceipt(TransactionReceipt Receipt, ReadOnlyMemory<byte> Content);

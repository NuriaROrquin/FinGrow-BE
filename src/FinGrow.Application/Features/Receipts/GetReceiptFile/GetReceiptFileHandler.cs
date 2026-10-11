namespace FinGrow.Application.Features.Receipts.GetReceiptFile;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class GetReceiptFileHandler(
    ITransactionReceiptRepository receipts,
    IFileStorage storage,
    ICurrentUser currentUser,
    ILogger<GetReceiptFileHandler> logger)
    : IRequestHandler<GetReceiptFileQuery, Result<ReceiptFile>>
{
    public async Task<Result<ReceiptFile>> Handle(GetReceiptFileQuery request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.UserId!.Value;

        var receipt = await receipts.GetByIdAsync(request.Id, cancellationToken);

        if (receipt is null)
        {
            return Result.Failure<ReceiptFile>(ReceiptErrors.NotFound(request.Id));
        }

        if (receipt.EmployeeId != employeeId)
        {
            return Result.Failure<ReceiptFile>(ReceiptErrors.NotOwned);
        }

        StoredFile? file;

        try
        {
            file = await storage.OpenAsync(receipt.StorageKey, cancellationToken);
        }
        catch (FileStorageUnavailableException exception)
        {
            LogStorageUnavailable(logger, exception, receipt.Id);
            return Result.Failure<ReceiptFile>(ReceiptErrors.StorageUnavailable);
        }

        if (file is null)
        {
            LogFileMissing(logger, receipt.Id, receipt.StorageKey);
            return Result.Failure<ReceiptFile>(ReceiptErrors.FileMissing(receipt.Id));
        }

        return Result.Success(new ReceiptFile(file.Content, receipt.ContentType, receipt.FileName));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo leer el comprobante {ReceiptId}: el almacenamiento no responde.")]
    private static partial void LogStorageUnavailable(ILogger logger, Exception exception, Guid receiptId);

    [LoggerMessage(Level = LogLevel.Error, Message = "El comprobante {ReceiptId} figura en la base pero no hay archivo en {StorageKey}.")]
    private static partial void LogFileMissing(ILogger logger, Guid receiptId, string storageKey);
}

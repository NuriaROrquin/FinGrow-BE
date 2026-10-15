namespace FinGrow.Application.Features.Receipts.Cleanup;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.Logging;

internal sealed partial class ReceiptsCleanupJob : IScheduledJob
{
    public const string JobName = "receipts-cleanup";

    private readonly ITransactionReceiptRepository _receipts;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ReceiptsCleanupOptions _options;
    private readonly ILogger<ReceiptsCleanupJob> _logger;

    public ReceiptsCleanupJob(
        ITransactionReceiptRepository receipts,
        IFileStorage storage,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ReceiptsCleanupOptions options,
        ILogger<ReceiptsCleanupJob> logger)
    {
        _receipts = receipts;
        _storage = storage;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Borra los comprobantes sueltos (sin movimiento asociado) subidos hace mas que el periodo de gracia: "
        + "primero el archivo del almacenamiento y despues su fila. Si el almacenamiento falla, la fila queda "
        + "y la proxima corrida lo reintenta.";

    public string Schedule => "0 7 * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var cutoff = _clock.UtcNow - _options.GracePeriod;
        var detached = await _receipts.ListDetachedUploadedBeforeAsync(cutoff, cancellationToken);
        var failures = new List<string>();
        var deleted = 0;

        foreach (var receipt in detached)
        {
            try
            {
                await _storage.DeleteAsync(receipt.StorageKey, cancellationToken);
            }
            catch (FileStorageUnavailableException exception)
            {
                LogDeleteFailed(_logger, exception, receipt.Id);
                failures.Add($"Comprobante {receipt.Id}: {exception.Message}");
                continue;
            }

            _receipts.Remove(receipt);
            deleted++;
        }

        if (deleted > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{detached.Count} comprobante(s) suelto(s) con mas de {_options.GraceDays} dia(s): {deleted} borrado(s), {failures.Count} con error.");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo borrar el archivo del comprobante suelto {ReceiptId}; se reintenta en la proxima corrida.")]
    private static partial void LogDeleteFailed(ILogger logger, Exception exception, Guid receiptId);
}

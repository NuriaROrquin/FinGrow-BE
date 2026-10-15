namespace FinGrow.Application.Features.Receipts.Scanning;

using System.Globalization;
using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal enum ScanOutcome
{
    Proposed,
    AlreadyProposed,
    Unreadable,
    MissingAmount,
    UnsupportedDocument,
    TooLarge,
    AiUnavailable
}

internal sealed record ScanProposal(ScanOutcome Outcome, Transaction? Transaction = null, bool AsksPaymentMethod = false)
{
    public static ScanProposal Of(ScanOutcome outcome) => new(outcome);
}

internal sealed partial class ReceiptScanner(
    IAiService ai,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<ReceiptScanner> logger)
{
    public const long MaxScannableBytes = 5 * 1024 * 1024;

    public const Currency DefaultCurrency = Currency.ARS;

    public const PaymentMethod DefaultPaymentMethod = PaymentMethod.Cash;

    public const string FallbackDescription = "Comprobante escaneado";

    public static string ExternalReferenceOf(TransactionReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        return string.Create(CultureInfo.InvariantCulture, $"receipt:{receipt.Id:N}");
    }

    public async Task<bool> WasProposedAsync(Guid employeeId, string externalReference, CancellationToken cancellationToken)
    {
        var known = await transactions.ListExistingExternalReferencesAsync(
            employeeId, TransactionSource.ReceiptScan, new[] { externalReference }, cancellationToken);

        return known.Count > 0;
    }

    public async Task<ScanProposal> ProposeAsync(
        TransactionReceipt receipt,
        ReadOnlyMemory<byte> content,
        string externalReference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        if (await WasProposedAsync(receipt.EmployeeId, externalReference, cancellationToken))
        {
            return ScanProposal.Of(ScanOutcome.AlreadyProposed);
        }

        if (content.Length > MaxScannableBytes)
        {
            return ScanProposal.Of(ScanOutcome.TooLarge);
        }

        var scan = await ScanAsync(receipt, content, cancellationToken);

        if (scan is null)
        {
            return ScanProposal.Of(ScanOutcome.AiUnavailable);
        }

        LogScan(logger, receipt.Id, content.Length, scan.Outcome);

        if (scan.Outcome != ReceiptScanOutcome.Scanned || scan.Receipt is not { } scanned)
        {
            return ScanProposal.Of(OutcomeOf(scan.Outcome));
        }

        var transaction = Register(receipt.EmployeeId, scanned, externalReference, scan.Model);

        transactions.Add(transaction);
        receipt.AttachTo(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ScanProposal(ScanOutcome.Proposed, transaction, AsksPaymentMethod: scanned.PaymentMethod is null);
    }

    private static ScanOutcome OutcomeOf(ReceiptScanOutcome outcome) =>
        outcome switch
        {
            ReceiptScanOutcome.Unreadable => ScanOutcome.Unreadable,
            ReceiptScanOutcome.MissingAmount => ScanOutcome.MissingAmount,
            ReceiptScanOutcome.UnsupportedDocument => ScanOutcome.UnsupportedDocument,
            ReceiptScanOutcome.TooLarge => ScanOutcome.TooLarge,
            _ => ScanOutcome.AiUnavailable
        };

    private Transaction Register(Guid employeeId, ScannedReceipt scanned, string externalReference, string? model)
    {
        var now = clock.UtcNow;
        var today = clock.Today;
        var occurredOn = scanned.OccurredOn is { } date && date <= today ? date : today;

        var transaction = Transaction.RegisterExpense(
            employeeId,
            Money.From(scanned.Amount, scanned.Currency),
            scanned.Category,
            Describe(scanned.Merchant),
            occurredOn,
            scanned.PaymentMethod ?? DefaultPaymentMethod,
            TransactionSource.ReceiptScan,
            TransactionStatus.Pending,
            now,
            externalReference);

        transaction.SuggestExpenseCategory(scanned.Category, scanned.Confidence, model, now);

        return transaction;
    }

    private static string Describe(string? merchant)
    {
        var trimmed = merchant?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return FallbackDescription;
        }

        return trimmed.Length > Transaction.MaxDescriptionLength ? trimmed[..Transaction.MaxDescriptionLength] : trimmed;
    }

    private async Task<ReceiptScan?> ScanAsync(
        TransactionReceipt receipt,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ai.ScanReceiptAsync(content, receipt.ContentType, DefaultCurrency, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogAiUnavailable(logger, exception, receipt.Id);
        }
        catch (JsonException exception)
        {
            LogAiUnavailable(logger, exception, receipt.Id);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogAiUnavailable(logger, exception, receipt.Id);
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Comprobante {ReceiptId} escaneado: {Bytes} bytes, resultado {Outcome}.")]
    private static partial void LogScan(ILogger logger, Guid receiptId, int bytes, ReceiptScanOutcome outcome);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FinGrow-AI no pudo escanear el comprobante {ReceiptId}; no se propuso ningun movimiento.")]
    private static partial void LogAiUnavailable(ILogger logger, Exception exception, Guid receiptId);
}

namespace FinGrow.Application.UnitTests.Features.Receipts;

using System.Globalization;
using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Receipts.ScanReceipt;
using FinGrow.Application.Features.Receipts.Scanning;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class ScanReceiptHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 14);
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

    private readonly FakeTransactionReceiptRepository _receipts = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeFileStorage _storage = new();
    private readonly FakeAiService _ai = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly FakeCurrentUser _user = new() { UserId = Guid.CreateVersion7() };

    [Fact]
    public async Task Proposes_a_pending_expense_with_what_the_ocr_read()
    {
        var receipt = StoreReceipt();

        var result = await Handle(receipt.Id);

        result.IsSuccess.ShouldBeTrue();
        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.EmployeeId.ShouldBe(_user.UserId!.Value);
        transaction.Source.ShouldBe(TransactionSource.ReceiptScan);
        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.Type.ShouldBe(TransactionType.Expense);
        transaction.Amount.ShouldBe(Money.From(1250.50m, Currency.ARS));
        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Alimentos);
        transaction.Description.ShouldBe("Supermercado Día");
        transaction.OccurredOn.ShouldBe(new DateOnly(2026, 10, 3));
        transaction.PaymentMethod.ShouldBe(PaymentMethod.DebitCard);
        transaction.AiConfidence.ShouldBe(0.93);
        transaction.AiModel.ShouldBe("fake-ocr+fake-model");
        transaction.ExternalReference.ShouldBe($"receipt:{receipt.Id:N}");
        receipt.TransactionId.ShouldBe(transaction.Id);
        result.Value.Id.ShouldBe(transaction.Id);
        result.Value.ReceiptId.ShouldBe(receipt.Id);
        result.Value.AiConfidenceLevel.ShouldBe(ConfidenceLevel.High);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Sends_the_stored_file_to_the_ocr_with_pesos_as_the_default_currency()
    {
        var receipt = StoreReceipt();

        await Handle(receipt.Id);

        var scanned = _ai.ScannedReceipts.ShouldHaveSingleItem();
        scanned.Document.ShouldBe(Jpeg);
        scanned.ContentType.ShouldBe("image/jpeg");
        scanned.DefaultCurrency.ShouldBe(Currency.ARS);
    }

    [Fact]
    public async Task Without_a_payment_method_on_the_receipt_it_proposes_cash()
    {
        _ai.ScanResult = Scanned(paymentMethod: null);

        await Handle(StoreReceipt().Id);

        _transactions.Transactions.Single().PaymentMethod.ShouldBe(ReceiptScanner.DefaultPaymentMethod);
    }

    [Fact]
    public async Task Without_a_merchant_it_uses_a_generic_description()
    {
        _ai.ScanResult = Scanned(merchant: null);

        await Handle(StoreReceipt().Id);

        _transactions.Transactions.Single().Description.ShouldBe(ReceiptScanner.FallbackDescription);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2026-10-20")]
    public async Task A_missing_or_future_date_becomes_today(string? date)
    {
        _ai.ScanResult = Scanned(occurredOn: date is null ? null : DateOnly.Parse(date, CultureInfo.InvariantCulture));

        await Handle(StoreReceipt().Id);

        _transactions.Transactions.Single().OccurredOn.ShouldBe(Today);
    }

    [Fact]
    public async Task Scanning_the_same_receipt_again_returns_the_pending_proposal()
    {
        var receipt = StoreReceipt();
        var first = await Handle(receipt.Id);

        var second = await Handle(receipt.Id);

        second.IsSuccess.ShouldBeTrue();
        second.Value.Id.ShouldBe(first.Value.Id);
        _transactions.Transactions.ShouldHaveSingleItem();
        _ai.ScannedReceipts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_receipt_already_attached_to_a_registered_movement_is_a_conflict()
    {
        var receipt = StoreReceipt();
        var manual = Transaction.RegisterExpense(
            _user.UserId!.Value,
            Money.From(500m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Almacén",
            Today,
            PaymentMethod.Cash,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now);
        _transactions.Add(manual);
        receipt.AttachTo(manual);

        var result = await Handle(receipt.Id);

        result.Error.Code.ShouldBe("Receipt.AlreadyAttached");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        _ai.ScannedReceipts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ReceiptScanOutcome.Unreadable, "Receipt.Unreadable")]
    [InlineData(ReceiptScanOutcome.MissingAmount, "Receipt.MissingAmount")]
    [InlineData(ReceiptScanOutcome.UnsupportedDocument, "Receipt.UnsupportedForScan")]
    [InlineData(ReceiptScanOutcome.TooLarge, "Receipt.TooLargeToScan")]
    public async Task A_receipt_the_ocr_cannot_read_keeps_the_image_and_proposes_nothing(
        ReceiptScanOutcome outcome,
        string code)
    {
        _ai.ScanResult = ReceiptScan.Of(outcome, "fake-ocr");
        var receipt = StoreReceipt();

        var result = await Handle(receipt.Id);

        result.Error.Code.ShouldBe(code);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        _transactions.Transactions.ShouldBeEmpty();
        receipt.TransactionId.ShouldBeNull();
        _storage.Files.ShouldContainKey(receipt.StorageKey);
    }

    [Fact]
    public async Task When_the_ai_service_does_not_respond_the_scan_is_unavailable()
    {
        _ai.Unreachable = true;

        var result = await Handle(StoreReceipt().Id);

        result.Error.Code.ShouldBe("Receipt.ScanUnavailable");
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_file_larger_than_what_the_ocr_accepts_is_not_sent()
    {
        var huge = new byte[ReceiptScanner.MaxScannableBytes + 1];
        Jpeg.CopyTo(huge, 0);

        var result = await Handle(StoreReceipt(huge).Id);

        result.Error.Code.ShouldBe("Receipt.TooLargeToScan");
        _ai.ScannedReceipts.ShouldBeEmpty();
    }

    [Fact]
    public async Task When_the_storage_does_not_respond_the_scan_is_unavailable()
    {
        var receipt = StoreReceipt();
        _storage.Unreachable = true;

        var result = await Handle(receipt.Id);

        result.Error.Type.ShouldBe(ErrorType.Unavailable);
        _ai.ScannedReceipts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Another_employees_receipt_is_forbidden()
    {
        var receipt = StoreReceipt(owner: Guid.CreateVersion7());

        var result = await Handle(receipt.Id);

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _ai.ScannedReceipts.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_unknown_receipt_is_not_found()
    {
        var result = await Handle(Guid.CreateVersion7());

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    private TransactionReceipt StoreReceipt(byte[]? content = null, Guid? owner = null)
    {
        var bytes = content ?? Jpeg;
        var receipt = TransactionReceipt.Upload(owner ?? _user.UserId!.Value, "image/jpeg", bytes.Length, "ticket.jpg", Now);
        _receipts.Add(receipt);
        _storage.Files[receipt.StorageKey] = (bytes, "image/jpeg");

        return receipt;
    }

    private static ReceiptScan Scanned(
        string? merchant = "Supermercado Día",
        PaymentMethod? paymentMethod = PaymentMethod.DebitCard,
        DateOnly? occurredOn = null) =>
        new(
            ReceiptScanOutcome.Scanned,
            new ScannedReceipt(merchant, 1250.50m, Currency.ARS, occurredOn, ExpenseCategory.Alimentos, paymentMethod, 0.93),
            "fake-ocr+fake-model");

    private Task<Result<TransactionResponse>> Handle(Guid receiptId)
    {
        var scanner = new ReceiptScanner(_ai, _transactions, _unitOfWork, _clock, NullLogger<ReceiptScanner>.Instance);
        var handler = new ScanReceiptHandler(
            _receipts, _transactions, _storage, scanner, _user, NullLogger<ScanReceiptHandler>.Instance);

        return handler.Handle(new ScanReceiptCommand(receiptId), CancellationToken.None);
    }
}

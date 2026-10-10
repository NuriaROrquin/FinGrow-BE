namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class TransactionReceiptTests
{
    private static readonly DateTimeOffset UploadedAt = new(2026, 10, 8, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    [Fact]
    public void A_new_receipt_is_not_attached_and_its_key_is_scoped_to_the_employee()
    {
        var receipt = TransactionReceipt.Upload(EmployeeId, "image/jpeg", 2048, "ticket.jpg", UploadedAt);

        receipt.EmployeeId.ShouldBe(EmployeeId);
        receipt.TransactionId.ShouldBeNull();
        receipt.StorageKey.ShouldBe($"receipts/{EmployeeId:N}/{receipt.Id:N}");
        receipt.ContentType.ShouldBe("image/jpeg");
        receipt.SizeBytes.ShouldBe(2048);
        receipt.FileName.ShouldBe("ticket.jpg");
        receipt.UploadedAt.ShouldBe(UploadedAt);
    }

    [Fact]
    public void The_content_type_is_stored_in_lower_case()
    {
        var receipt = TransactionReceipt.Upload(EmployeeId, "Image/PNG", 10, null, UploadedAt);

        receipt.ContentType.ShouldBe("image/png");
    }

    [Theory]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void Only_the_file_name_is_kept_without_any_path(string? fileName, string? expected)
    {
        var receipt = TransactionReceipt.Upload(EmployeeId, "application/pdf", 10, fileName, UploadedAt);

        receipt.FileName.ShouldBe(expected);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("text/html")]
    [InlineData("")]
    public void An_unsupported_content_type_is_rejected(string contentType)
    {
        Should.Throw<DomainException>(() =>
            TransactionReceipt.Upload(EmployeeId, contentType, 10, null, UploadedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(TransactionReceipt.MaxSizeBytes + 1)]
    public void A_size_outside_the_limits_is_rejected(long sizeBytes)
    {
        Should.Throw<DomainException>(() =>
            TransactionReceipt.Upload(EmployeeId, "image/png", sizeBytes, null, UploadedAt));
    }

    [Fact]
    public void A_receipt_without_employee_is_rejected()
    {
        Should.Throw<DomainException>(() =>
            TransactionReceipt.Upload(Guid.Empty, "image/png", 10, null, UploadedAt));
    }

    [Theory]
    [InlineData("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData("IMAGE/JPEG", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 })]
    [InlineData("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 })]
    public void The_content_matches_the_signature_of_its_declared_type(string contentType, byte[] content)
    {
        TransactionReceipt.MatchesSignature(contentType, content).ShouldBeTrue();
    }

    [Theory]
    [InlineData("image/png", new byte[] { 0x3C, 0x68, 0x74, 0x6D, 0x6C, 0x3E, 0x00, 0x00 })]
    [InlineData("image/png", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData("image/jpeg", new byte[] { 0xFF, 0xD8 })]
    [InlineData("image/gif", new byte[] { 0x47, 0x49, 0x46, 0x38 })]
    public void Content_that_does_not_start_with_the_signature_of_its_type_does_not_match(string contentType, byte[] content)
    {
        TransactionReceipt.MatchesSignature(contentType, content).ShouldBeFalse();
    }

    [Fact]
    public void Attaching_links_the_receipt_to_the_transaction()
    {
        var receipt = TransactionReceipt.Upload(EmployeeId, "image/png", 10, null, UploadedAt);
        var transaction = NewExpense(EmployeeId);

        receipt.AttachTo(transaction);

        receipt.TransactionId.ShouldBe(transaction.Id);
    }

    [Fact]
    public void Attaching_twice_to_the_same_transaction_is_harmless()
    {
        var receipt = TransactionReceipt.Upload(EmployeeId, "image/png", 10, null, UploadedAt);
        var transaction = NewExpense(EmployeeId);
        receipt.AttachTo(transaction);

        receipt.AttachTo(transaction);

        receipt.TransactionId.ShouldBe(transaction.Id);
    }

    [Fact]
    public void Attaching_to_another_transaction_moves_the_receipt()
    {
        var receipt = TransactionReceipt.Upload(EmployeeId, "image/png", 10, null, UploadedAt);
        receipt.AttachTo(NewExpense(EmployeeId));
        var correct = NewExpense(EmployeeId);

        receipt.AttachTo(correct);

        receipt.TransactionId.ShouldBe(correct.Id);
    }

    [Fact]
    public void A_receipt_cannot_be_attached_to_a_transaction_of_another_employee()
    {
        var receipt = TransactionReceipt.Upload(EmployeeId, "image/png", 10, null, UploadedAt);

        Should.Throw<DomainException>(() => receipt.AttachTo(NewExpense(Guid.CreateVersion7())));
        receipt.TransactionId.ShouldBeNull();
    }

    private static Transaction NewExpense(Guid employeeId) => Transaction.RegisterExpense(
        employeeId,
        Money.From(4500m, Currency.ARS),
        ExpenseCategory.Alimentos,
        "Supermercado",
        new DateOnly(2026, 10, 7),
        PaymentMethod.DebitCard,
        TransactionSource.Manual,
        TransactionStatus.Confirmed,
        UploadedAt);
}

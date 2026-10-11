namespace FinGrow.Application.UnitTests.Features.Receipts;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Receipts.GetReceiptFile;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

public class GetReceiptFileHandlerTests
{
    private static readonly DateTimeOffset UploadedAt = new(2026, 10, 8, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeTransactionReceiptRepository _receipts = new();
    private readonly FakeFileStorage _storage = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.CreateVersion7() };

    [Fact]
    public async Task Returns_the_file_of_the_employees_own_receipt()
    {
        var receipt = AddStoredReceipt(_user.UserId!.Value, new byte[] { 0xFF, 0xD8, 0xFF, 0x01 });

        var result = await Handle(receipt.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ContentType.ShouldBe("image/jpeg");
        result.Value.FileName.ShouldBe("ticket.jpg");
        await using var content = result.Value.Content;
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy);
        copy.ToArray().ShouldBe(new byte[] { 0xFF, 0xD8, 0xFF, 0x01 });
    }

    [Fact]
    public async Task Returns_forbidden_for_a_receipt_of_another_employee()
    {
        var receipt = AddStoredReceipt(Guid.CreateVersion7(), new byte[] { 0xFF, 0xD8, 0xFF });

        var result = await Handle(receipt.Id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Returns_not_found_for_an_unknown_receipt()
    {
        var result = await Handle(Guid.CreateVersion7());

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Returns_not_found_when_the_file_is_missing_from_the_storage()
    {
        var receipt = AddStoredReceipt(_user.UserId!.Value, new byte[] { 0xFF, 0xD8, 0xFF });
        _storage.Files.Clear();

        var result = await Handle(receipt.Id);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Returns_unavailable_when_the_storage_does_not_respond()
    {
        var receipt = AddStoredReceipt(_user.UserId!.Value, new byte[] { 0xFF, 0xD8, 0xFF });
        _storage.Unreachable = true;

        var result = await Handle(receipt.Id);

        result.Error.Type.ShouldBe(ErrorType.Unavailable);
    }

    private TransactionReceipt AddStoredReceipt(Guid employeeId, byte[] content)
    {
        var receipt = TransactionReceipt.Upload(employeeId, "image/jpeg", content.Length, "ticket.jpg", UploadedAt);
        _receipts.Receipts.Add(receipt);
        _storage.Files[receipt.StorageKey] = (content, receipt.ContentType);

        return receipt;
    }

    private Task<Result<ReceiptFile>> Handle(Guid id) =>
        new GetReceiptFileHandler(_receipts, _storage, _user, NullLogger<GetReceiptFileHandler>.Instance)
            .Handle(new GetReceiptFileQuery(id), CancellationToken.None);
}

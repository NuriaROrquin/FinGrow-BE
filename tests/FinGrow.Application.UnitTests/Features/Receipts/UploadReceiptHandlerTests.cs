namespace FinGrow.Application.UnitTests.Features.Receipts;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Receipts.UploadReceipt;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

public class UploadReceiptHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 15, 0, 0, TimeSpan.Zero);
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01 };

    private readonly FakeTransactionReceiptRepository _receipts = new();
    private readonly FakeFileStorage _storage = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.CreateVersion7() };

    [Fact]
    public async Task Stores_the_file_and_persists_only_its_reference()
    {
        var result = await Handle(Png, "image/png", "ticket.png");

        result.IsSuccess.ShouldBeTrue();
        var receipt = _receipts.Receipts.ShouldHaveSingleItem();
        receipt.EmployeeId.ShouldBe(_user.UserId!.Value);
        receipt.SizeBytes.ShouldBe(Png.Length);
        receipt.TransactionId.ShouldBeNull();
        _storage.Files[receipt.StorageKey].Content.ShouldBe(Png);
        _storage.Files[receipt.StorageKey].ContentType.ShouldBe("image/png");
        _unitOfWork.SaveCount.ShouldBe(1);
        result.Value.Id.ShouldBe(receipt.Id);
        result.Value.FileName.ShouldBe("ticket.png");
    }

    [Fact]
    public async Task Rejects_content_that_does_not_match_the_declared_type()
    {
        var html = "<html><script>alert(1)</script></html>"u8.ToArray();

        var result = await Handle(html, "image/png");

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        _storage.Files.ShouldBeEmpty();
        _receipts.Receipts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejects_a_file_larger_than_the_limit_even_if_the_declared_length_lies()
    {
        var huge = new byte[TransactionReceipt.MaxSizeBytes + 1];
        Png.CopyTo(huge, 0);

        var result = await Handle(huge, "image/png", declaredLength: 100);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        _storage.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Responds_unavailable_and_saves_nothing_when_the_storage_does_not_respond()
    {
        _storage.Unreachable = true;

        var result = await Handle(Png, "image/png");

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
        _receipts.Receipts.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Deletes_the_stored_file_when_the_database_fails()
    {
        var handler = new UploadReceiptHandler(
            _receipts, _storage, new FailingUnitOfWork(), _user, new FakeDateTimeProvider(Now),
            NullLogger<UploadReceiptHandler>.Instance);
        using var content = new MemoryStream(Png);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(new UploadReceiptCommand(content, "image/png", Png.Length, null), CancellationToken.None));

        _storage.Files.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("image/gif", 10)]
    [InlineData("image/png", 0)]
    [InlineData("image/png", TransactionReceipt.MaxSizeBytes + 1)]
    public void The_validator_rejects_unsupported_types_and_sizes(string contentType, long length)
    {
        var validation = new UploadReceiptValidator().Validate(
            new UploadReceiptCommand(Stream.Null, contentType, length, null));

        validation.IsValid.ShouldBeFalse();
    }

    private async Task<Result<TransactionReceiptResponse>> Handle(
        byte[] bytes,
        string contentType,
        string? fileName = null,
        long? declaredLength = null)
    {
        var handler = new UploadReceiptHandler(
            _receipts, _storage, _unitOfWork, _user, new FakeDateTimeProvider(Now),
            NullLogger<UploadReceiptHandler>.Instance);
        using var content = new MemoryStream(bytes);

        return await handler.Handle(
            new UploadReceiptCommand(content, contentType, declaredLength ?? bytes.Length, fileName),
            CancellationToken.None);
    }

    private sealed class FailingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("la base no responde");
    }
}

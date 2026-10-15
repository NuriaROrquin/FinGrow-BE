namespace FinGrow.Application.UnitTests.Features.Receipts;

using System.ComponentModel.DataAnnotations;
using FinGrow.Application.Features.Receipts.Cleanup;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class ReceiptsCleanupJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 7, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeTransactionReceiptRepository _receipts = new();
    private readonly FakeFileStorage _storage = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task A_detached_receipt_older_than_the_grace_period_loses_its_file_and_its_row()
    {
        var abandoned = Stored(uploadedDaysAgo: 8);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _storage.Files.ShouldNotContainKey(abandoned.StorageKey);
        _receipts.Receipts.ShouldNotContain(abandoned);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_detached_receipt_within_the_grace_period_is_left_alone()
    {
        var justUploaded = Stored(uploadedDaysAgo: 6);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _storage.Files.ShouldContainKey(justUploaded.StorageKey);
        _receipts.Receipts.ShouldContain(justUploaded);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_attached_receipt_is_never_deleted_however_old_it_is()
    {
        var attached = Stored(uploadedDaysAgo: 400);
        attached.AttachTo(Expense());

        await Job().ExecuteAsync(CancellationToken.None);

        _storage.Files.ShouldContainKey(attached.StorageKey);
        _receipts.Receipts.ShouldContain(attached);
    }

    [Fact]
    public async Task A_receipt_whose_file_cannot_be_deleted_keeps_its_row_and_the_rest_are_cleaned_up()
    {
        var stuck = Stored(uploadedDaysAgo: 10);
        var removable = Stored(uploadedDaysAgo: 9);
        _storage.UndeletableKeys.Add(stuck.StorageKey);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe($"Comprobante {stuck.Id}: el almacenamiento no pudo borrar {stuck.StorageKey}");
        result.Summary.ShouldBe("2 comprobante(s) suelto(s) con mas de 7 dia(s): 1 borrado(s), 1 con error.");
        _receipts.Receipts.ShouldBe(new[] { stuck });
        _storage.Files.ShouldNotContainKey(removable.StorageKey);
    }

    [Fact]
    public async Task The_next_run_retries_a_receipt_that_failed_before()
    {
        var stuck = Stored(uploadedDaysAgo: 10);
        _storage.UndeletableKeys.Add(stuck.StorageKey);
        await Job().ExecuteAsync(CancellationToken.None);

        _storage.UndeletableKeys.Clear();
        var retried = await Job().ExecuteAsync(CancellationToken.None);

        retried.IsSuccess.ShouldBeTrue();
        _receipts.Receipts.ShouldBeEmpty();
        _storage.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_row_whose_file_is_already_gone_is_still_removed()
    {
        var orphanRow = Stored(uploadedDaysAgo: 30);
        _storage.Files.Remove(orphanRow.StorageKey);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _receipts.Receipts.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_summary_says_how_many_receipts_were_deleted()
    {
        Stored(uploadedDaysAgo: 8);
        Stored(uploadedDaysAgo: 15);
        Stored(uploadedDaysAgo: 2);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.Summary.ShouldBe("2 comprobante(s) suelto(s) con mas de 7 dia(s): 2 borrado(s), 0 con error.");
    }

    [Fact]
    public async Task The_grace_period_comes_from_the_configuration()
    {
        var weekOld = Stored(uploadedDaysAgo: 8);
        var monthOld = Stored(uploadedDaysAgo: 31);

        var result = await Job(new ReceiptsCleanupOptions { GraceDays = 30 }).ExecuteAsync(CancellationToken.None);

        _receipts.Receipts.ShouldBe(new[] { weekOld });
        _storage.Files.ShouldNotContainKey(monthOld.StorageKey);
        result.Summary.ShouldBe("1 comprobante(s) suelto(s) con mas de 30 dia(s): 1 borrado(s), 0 con error.");
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(365, true)]
    [InlineData(366, false)]
    public void The_grace_period_goes_from_one_day_to_a_year(int graceDays, bool expected)
    {
        var options = new ReceiptsCleanupOptions { GraceDays = graceDays };

        Validator.TryValidateObject(options, new ValidationContext(options), null, validateAllProperties: true).ShouldBe(expected);
    }

    [Fact]
    public void The_job_is_registered_with_a_daily_schedule_and_a_week_of_grace_by_default()
    {
        var job = Job();

        job.Name.ShouldBe("receipts-cleanup");
        job.Schedule.ShouldBe("0 7 * * *");
        job.Description.ShouldNotBeNullOrWhiteSpace();
        new ReceiptsCleanupOptions().GraceDays.ShouldBe(7);
    }

    private ReceiptsCleanupJob Job(ReceiptsCleanupOptions? options = null) => new(
        _receipts,
        _storage,
        _unitOfWork,
        _clock,
        options ?? new ReceiptsCleanupOptions(),
        NullLogger<ReceiptsCleanupJob>.Instance);

    private TransactionReceipt Stored(int uploadedDaysAgo)
    {
        var receipt = TransactionReceipt.Upload(EmployeeId, "image/jpeg", 1024, "ticket.jpg", Now.AddDays(-uploadedDaysAgo));
        _receipts.Add(receipt);
        _storage.Files[receipt.StorageKey] = (new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg");

        return receipt;
    }

    private static Transaction Expense() => Transaction.RegisterExpense(
        EmployeeId,
        Money.From(1500m, Currency.ARS),
        ExpenseCategory.Alimentos,
        "Supermercado",
        new DateOnly(2025, 9, 1),
        PaymentMethod.DebitCard,
        TransactionSource.ReceiptScan,
        TransactionStatus.Confirmed,
        Now.AddDays(-400));
}

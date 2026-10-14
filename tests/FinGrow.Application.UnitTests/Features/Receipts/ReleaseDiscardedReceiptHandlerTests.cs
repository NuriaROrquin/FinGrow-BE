namespace FinGrow.Application.UnitTests.Features.Receipts;

using FinGrow.Application.Events;
using FinGrow.Application.Features.Receipts.ReleaseDiscarded;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;

public class ReleaseDiscardedReceiptHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeTransactionReceiptRepository _receipts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Discarding_a_scanned_proposal_releases_its_receipt_for_the_cleanup()
    {
        var proposal = Proposal();
        var receipt = TransactionReceipt.Upload(EmployeeId, "image/jpeg", 100, "ticket.jpg", Now);
        receipt.AttachTo(proposal);
        _receipts.Add(receipt);

        await Handle(proposal.Id);

        receipt.TransactionId.ShouldBeNull();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_discarded_proposal_without_receipt_saves_nothing()
    {
        await Handle(Proposal().Id);

        _unitOfWork.SaveCount.ShouldBe(0);
    }

    private static Transaction Proposal() => Transaction.RegisterExpense(
        EmployeeId,
        Money.From(1250.50m, Currency.ARS),
        ExpenseCategory.Alimentos,
        "Supermercado Día",
        new DateOnly(2026, 10, 14),
        PaymentMethod.DebitCard,
        TransactionSource.ReceiptScan,
        TransactionStatus.Pending,
        Now);

    private Task Handle(Guid transactionId) =>
        new ReleaseDiscardedReceiptHandler(_receipts, _unitOfWork).Handle(
            new DomainEventEnvelope<TransactionDiscarded>(new TransactionDiscarded(transactionId, EmployeeId, Now)),
            CancellationToken.None);
}

namespace FinGrow.Application.UnitTests.Features.Transactions.PendingReview;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

internal static class PendingTransactionFactory
{
    public static readonly DateTimeOffset ProposedAt = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    public static Transaction PendingExpense(
        Guid employeeId,
        TransactionSource source = TransactionSource.MercadoPago,
        string description = "Supermercado Coto",
        DateOnly? occurredOn = null,
        string? externalReference = null) => Transaction.RegisterExpense(
        employeeId,
        Money.From(15400.50m, Currency.ARS),
        ExpenseCategory.Otros,
        description,
        occurredOn ?? new DateOnly(2026, 10, 4),
        PaymentMethod.DebitCard,
        source,
        TransactionStatus.Pending,
        ProposedAt,
        externalReference);

    public static Transaction PendingIncome(Guid employeeId) => Transaction.RegisterIncome(
        employeeId,
        Money.From(6000m, Currency.ARS),
        IncomeCategory.Otros,
        "Cobro por Mercado Pago",
        new DateOnly(2026, 10, 3),
        PaymentMethod.DigitalWallet,
        TransactionSource.MercadoPago,
        TransactionStatus.Pending,
        ProposedAt);
}

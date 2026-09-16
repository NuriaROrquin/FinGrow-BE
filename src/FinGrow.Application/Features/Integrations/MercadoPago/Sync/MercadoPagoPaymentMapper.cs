namespace FinGrow.Application.Features.Integrations.MercadoPago.Sync;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

internal static class MercadoPagoPaymentMapper
{
    internal const string ApprovedStatus = "approved";
    internal const string OwnMoneyIn = "account_fund";
    internal const string FallbackDescription = "Movimiento de Mercado Pago";

    private static readonly TimeSpan ArgentinaUtcOffset = TimeSpan.FromHours(-3);

    public static string ExternalReference(MercadoPagoPayment payment) => payment.Id.ToString(CultureInfo.InvariantCulture);

    public static bool IsImportable(MercadoPagoPayment payment) =>
        payment.Status == ApprovedStatus
        && payment.OperationType != OwnMoneyIn
        && payment.TransactionAmount > 0
        && Enum.TryParse<Currency>(payment.CurrencyId, ignoreCase: false, out _);

    public static Transaction ToPendingTransaction(MercadoPagoPayment payment, EmployeeIntegration integration, DateTimeOffset now)
    {
        var amount = Money.From(payment.TransactionAmount, Enum.Parse<Currency>(payment.CurrencyId));
        var occurredOn = DateOnly.FromDateTime((payment.DateApproved ?? payment.DateCreated).ToOffset(ArgentinaUtcOffset).DateTime);
        var description = string.IsNullOrWhiteSpace(payment.Description) ? FallbackDescription : payment.Description;
        var paymentMethod = ToPaymentMethod(payment.PaymentTypeId);
        var isIncome = payment.CollectorId?.ToString(CultureInfo.InvariantCulture) == integration.ExternalAccountId;

        return isIncome
            ? Transaction.RegisterIncome(
                integration.EmployeeId,
                amount,
                IncomeCategory.Otros,
                description,
                occurredOn,
                paymentMethod,
                TransactionSource.MercadoPago,
                TransactionStatus.Pending,
                now,
                ExternalReference(payment))
            : Transaction.RegisterExpense(
                integration.EmployeeId,
                amount,
                ExpenseCategory.Otros,
                description,
                occurredOn,
                paymentMethod,
                TransactionSource.MercadoPago,
                TransactionStatus.Pending,
                now,
                ExternalReference(payment));
    }

    internal static PaymentMethod ToPaymentMethod(string? paymentTypeId) => paymentTypeId switch
    {
        "credit_card" => PaymentMethod.CreditCard,
        "debit_card" => PaymentMethod.DebitCard,
        "bank_transfer" => PaymentMethod.BankTransfer,
        "ticket" or "atm" => PaymentMethod.Cash,
        _ => PaymentMethod.DigitalWallet
    };
}

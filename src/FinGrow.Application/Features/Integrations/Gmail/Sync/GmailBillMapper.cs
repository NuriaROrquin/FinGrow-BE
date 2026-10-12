namespace FinGrow.Application.Features.Integrations.Gmail.Sync;

using System.Globalization;
using System.Text;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

internal static class GmailBillMapper
{
    internal const PaymentMethod DefaultPaymentMethod = PaymentMethod.BankTransfer;

    private const int MaxIssuerKeyLength = 60;

    /// <summary>
    /// Identifica la factura por empresa, vencimiento e importe y no por el id del mail: la original y su
    /// reenvio son dos mails distintos de la misma factura y no tienen que proponerse dos veces.
    /// Sin vencimiento se usa el mes en que llego el mail.
    /// </summary>
    public static string ExternalReference(ParsedBill bill, DateTimeOffset receivedAt)
    {
        var due = bill.DueDate is { } dueDate
            ? dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : "sin-vencimiento-" + ArgentinaTime.DateOf(receivedAt).ToString("yyyy-MM", CultureInfo.InvariantCulture);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"gmail:{IssuerKey(bill.Issuer)}:{due}:{bill.Amount:0.00}");
    }

    public static Transaction ToPendingExpense(
        ParsedBill bill,
        GmailMessage message,
        Guid employeeId,
        string? model,
        DateTimeOffset now)
    {
        var transaction = Transaction.RegisterExpense(
            employeeId,
            Money.From(bill.Amount, bill.Currency),
            ExpenseCategory.Otros,
            Description(bill),
            ArgentinaTime.DateOf(message.ReceivedAt),
            DefaultPaymentMethod,
            TransactionSource.Gmail,
            TransactionStatus.Pending,
            now,
            ExternalReference(bill, message.ReceivedAt));

        if (ConfidenceScale.IsValid(bill.Confidence))
        {
            transaction.SuggestExpenseCategory(bill.Category, bill.Confidence, model, now);
        }

        return transaction;
    }

    internal static string Description(ParsedBill bill) =>
        bill.DueDate is { } dueDate
            ? string.Create(CultureInfo.InvariantCulture, $"Factura {bill.Issuer} (vence {dueDate:dd/MM/yyyy})")
            : $"Factura {bill.Issuer}";

    // "MetroGAS", "Metrogas" y "metrogás" tienen que dar la misma clave. Sufijos como "S.A." los saca
    internal static string IssuerKey(string issuer)
    {
        var builder = new StringBuilder(issuer.Length);

        foreach (var character in issuer.Normalize(NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.Length > MaxIssuerKeyLength ? builder.ToString(0, MaxIssuerKeyLength) : builder.ToString();
    }
}

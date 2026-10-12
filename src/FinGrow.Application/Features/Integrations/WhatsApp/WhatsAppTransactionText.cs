namespace FinGrow.Application.Features.Integrations.WhatsApp;

using System.Globalization;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Domain.Entities;

internal static class WhatsAppTransactionText
{
    public const string HelpReply =
        "Para registrar un movimiento escribime o mandame un audio contando qué fue y cuánto. Por ejemplo:\n" +
        "• Gasté $500 en el súper\n" +
        "• Pagué $1500 el almuerzo con débito\n" +
        "• Cobré $10000 por un freelance";

    public static string Proposal(Transaction transaction, bool asksPaymentMethod)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var summary = ChatTransactionText.Summary(transaction, asksPaymentMethod);

        if (!asksPaymentMethod)
        {
            return summary + "\n\n¿Lo registro? Respondé SÍ o NO.";
        }

        var options = ChatTransactionText.PaymentMethodsFor(transaction.Type)
            .Select((method, index) => string.Create(
                CultureInfo.InvariantCulture, $"{index + 1}. {ChatTransactionText.PaymentMethodLabel(method)}"));

        return summary + "\n\n" +
            ChatTransactionText.PaymentMethodQuestion(transaction.Type) + " Respondé con el número:\n" +
            string.Join('\n', options) + "\n\n" +
            "O respondé NO para descartarlo.";
    }
}

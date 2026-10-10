namespace FinGrow.Application.Features.Integrations.ChatTransactions;

using System.Globalization;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

internal static class ChatTransactionText
{
    public const string MissingAmountReply =
        "No encontré el monto. ¿Me lo mandás de nuevo con el importe? Por ejemplo: Gasté $500 en el súper.";

    public const string MultipleTransactionsReply =
        "Mandame un movimiento por mensaje así no se mezclan. Por ejemplo: Gasté $500 en el súper.";

    public const string HelpReply =
        "Para registrar un movimiento escribime qué fue y cuánto. Por ejemplo:\n" +
        "• Gasté $500 en el súper\n" +
        "• Pagué $1500 el almuerzo con débito\n" +
        "• Cobré $10000 por un freelance";

    public const string AiUnavailableReply =
        "No pude procesar tu mensaje en este momento. Probá de nuevo en unos minutos.";

    public const string DiscardedReply = "❌ Descartado: no se registró nada.";

    public const string AlreadyReviewedReply =
        "Este movimiento ya fue revisado. Si querés cambiarlo, hacelo desde la app (Transacciones).";

    public const string ConfirmLabel = "✅ Confirmar";

    public const string DiscardLabel = "❌ Descartar";

    private static readonly PaymentMethod[] ExpensePaymentMethods =
    {
        PaymentMethod.Cash,
        PaymentMethod.DebitCard,
        PaymentMethod.CreditCard,
        PaymentMethod.BankTransfer,
        PaymentMethod.DigitalWallet
    };

    private static readonly PaymentMethod[] IncomePaymentMethods =
    {
        PaymentMethod.Cash,
        PaymentMethod.BankTransfer,
        PaymentMethod.DigitalWallet
    };

    public static IReadOnlyList<PaymentMethod> PaymentMethodsFor(TransactionType type) =>
        type == TransactionType.Income ? IncomePaymentMethods : ExpensePaymentMethods;

    public static bool IsAllowed(TransactionType type, PaymentMethod? paymentMethod) =>
        paymentMethod is { } method && PaymentMethodsFor(type).Contains(method);

    public static string Proposal(Transaction transaction, bool asksPaymentMethod)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var isIncome = transaction.Type == TransactionType.Income;
        var lines = new List<string>
        {
            (isIncome ? "💰 Ingreso: " : "💸 Gasto: ") + FormatAmount(transaction.Amount),
            "🏷️ Categoría: " + CategoryLabel(transaction),
            "📝 " + transaction.Description
        };

        if (!asksPaymentMethod)
        {
            lines.Add("💳 " + PaymentMethodLabel(transaction.PaymentMethod));
        }

        lines.Add(string.Empty);
        lines.Add(asksPaymentMethod
            ? isIncome ? "¿Cómo lo cobraste? Elegí uno para confirmarlo." : "¿Cómo lo pagaste? Elegí uno para confirmarlo."
            : "¿Lo registro?");

        return string.Join('\n', lines);
    }

    public static string Confirmed(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var kind = transaction.Type == TransactionType.Income ? "Ingreso" : "Gasto";

        return "✅ Registrado: " + kind + " de " + FormatAmount(transaction.Amount) + " en " + CategoryLabel(transaction) +
            " (" + PaymentMethodLabel(transaction.PaymentMethod) + "). Ya lo ves en la app.";
    }

    public static string PaymentMethodLabel(PaymentMethod paymentMethod) =>
        paymentMethod switch
        {
            PaymentMethod.Cash => "Efectivo",
            PaymentMethod.DebitCard => "Débito",
            PaymentMethod.CreditCard => "Crédito",
            PaymentMethod.BankTransfer => "Transferencia",
            PaymentMethod.DigitalWallet => "Billetera virtual",
            _ => paymentMethod.ToString()
        };

    public static string PaymentMethodButtonLabel(PaymentMethod paymentMethod) =>
        paymentMethod switch
        {
            PaymentMethod.Cash => "💵 Efectivo",
            PaymentMethod.DebitCard => "💳 Débito",
            PaymentMethod.CreditCard => "💳 Crédito",
            PaymentMethod.BankTransfer => "🏦 Transferencia",
            PaymentMethod.DigitalWallet => "📱 Billetera virtual",
            _ => paymentMethod.ToString()
        };

    public static string FormatAmount(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);

        var format = decimal.Truncate(amount.Amount) == amount.Amount ? "#,0" : "#,0.00";
        var number = amount.Amount
            .ToString(format, CultureInfo.InvariantCulture)
            .Replace(',', '_')
            .Replace('.', ',')
            .Replace('_', '.');

        var symbol = amount.Currency switch
        {
            Currency.USD => "US$",
            Currency.EUR => "€",
            Currency.BRL => "R$",
            _ => "$"
        };

        return symbol + " " + number + " (" + amount.Currency + ")";
    }

    private static string CategoryLabel(Transaction transaction) =>
        transaction.ExpenseCategory is { } expense
            ? expense switch
            {
                ExpenseCategory.Educacion => "Educación",
                ExpenseCategory.AhorroInversion => "Ahorro e inversión",
                _ => expense.ToString()
            }
            : transaction.IncomeCategory?.ToString() ?? string.Empty;
}

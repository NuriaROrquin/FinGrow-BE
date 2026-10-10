namespace FinGrow.Application.Features.Integrations.Telegram;

using System.Globalization;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

internal sealed record TelegramTransactionCallback(Guid TransactionId, ChatDecision Decision, PaymentMethod? PaymentMethod = null)
{
    private const string Prefix = "tx";
    private const string ConfirmAction = "ok";
    private const string DiscardAction = "no";
    private const string PaymentMethodAction = "pm";
    private const char Separator = ':';

    public string Encode()
    {
        var action = Decision == ChatDecision.Discard
            ? DiscardAction
            : PaymentMethod is { } method
                ? PaymentMethodAction + ((int)method).ToString(CultureInfo.InvariantCulture)
                : ConfirmAction;

        return string.Join(Separator, Prefix, action, TransactionId.ToString("N", CultureInfo.InvariantCulture));
    }

    public static TelegramTransactionCallback? Parse(string? data)
    {
        var parts = (data ?? string.Empty).Split(Separator);

        if (parts.Length != 3
            || !string.Equals(parts[0], Prefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(parts[2], "N", out var transactionId))
        {
            return null;
        }

        var action = parts[1];

        if (string.Equals(action, ConfirmAction, StringComparison.Ordinal))
        {
            return new TelegramTransactionCallback(transactionId, ChatDecision.Confirm);
        }

        if (string.Equals(action, DiscardAction, StringComparison.Ordinal))
        {
            return new TelegramTransactionCallback(transactionId, ChatDecision.Discard);
        }

        if (action.StartsWith(PaymentMethodAction, StringComparison.Ordinal)
            && int.TryParse(action.AsSpan(PaymentMethodAction.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            && Enum.IsDefined((PaymentMethod)value))
        {
            return new TelegramTransactionCallback(transactionId, ChatDecision.Confirm, (PaymentMethod)value);
        }

        return null;
    }

    public static IReadOnlyList<IReadOnlyList<TelegramButton>> KeyboardFor(Transaction transaction, bool asksPaymentMethod)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var discard = new TelegramButton(
            ChatTransactionText.DiscardLabel,
            new TelegramTransactionCallback(transaction.Id, ChatDecision.Discard).Encode());

        if (!asksPaymentMethod)
        {
            var confirm = new TelegramButton(
                ChatTransactionText.ConfirmLabel,
                new TelegramTransactionCallback(transaction.Id, ChatDecision.Confirm).Encode());

            return new[] { new[] { confirm, discard } };
        }

        var rows = ChatTransactionText.PaymentMethodsFor(transaction.Type)
            .Select(method => new TelegramButton(
                ChatTransactionText.PaymentMethodButtonLabel(method),
                new TelegramTransactionCallback(transaction.Id, ChatDecision.Confirm, method).Encode()))
            .Chunk(2)
            .Select(row => (IReadOnlyList<TelegramButton>)row)
            .ToList();

        rows.Add(new[] { discard });

        return rows;
    }
}

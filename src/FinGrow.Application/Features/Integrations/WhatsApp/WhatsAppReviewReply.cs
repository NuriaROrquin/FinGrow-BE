namespace FinGrow.Application.Features.Integrations.WhatsApp;

using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using FinGrow.Application.Features.Integrations.ChatTransactions;

internal sealed record WhatsAppReviewReply(ChatDecision Decision, int? Option = null)
{
    private const string ThumbsUp = "👍";

    private static readonly FrozenSet<string> ConfirmWords =
        new[] { "si", "s", "dale", "ok", "okey", "oka", "confirmar", "confirmo", "registralo" }
            .ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> DiscardWords =
        new[] { "no", "n", "descartar", "descartalo", "cancelar" }
            .ToFrozenSet(StringComparer.Ordinal);

    public static WhatsAppReviewReply? Parse(string? text)
    {
        var trimmed = (text ?? string.Empty).Trim();

        if (trimmed.StartsWith(ThumbsUp, StringComparison.Ordinal))
        {
            return new WhatsAppReviewReply(ChatDecision.Confirm);
        }

        var normalized = Normalize(trimmed);

        if (ConfirmWords.Contains(normalized))
        {
            return new WhatsAppReviewReply(ChatDecision.Confirm);
        }

        if (DiscardWords.Contains(normalized))
        {
            return new WhatsAppReviewReply(ChatDecision.Discard);
        }

        return normalized.Length == 1 && normalized[0] is >= '1' and <= '9'
            ? new WhatsAppReviewReply(ChatDecision.Confirm, normalized[0] - '0')
            : null;
    }

    private static string Normalize(string text)
    {
        var decomposed = text.ToLower(CultureInfo.InvariantCulture).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed.Where(char.IsLetterOrDigit))
        {
            builder.Append(character);
        }

        return builder.ToString();
    }
}

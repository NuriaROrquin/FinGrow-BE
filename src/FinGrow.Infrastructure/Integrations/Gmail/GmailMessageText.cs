namespace FinGrow.Infrastructure.Integrations.Gmail;

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// Saca el texto legible de un mail tal como lo devuelve la API de Gmail (format=full): un arbol MIME
/// con las partes en base64url. Las facturas suelen venir solo en HTML, con el total dentro de una tabla.
/// </summary>
internal static partial class GmailMessageText
{
    private const int MinUsefulPlainTextLength = 200;

    public static string Extract(GmailPayload? payload)
    {
        if (payload is null)
        {
            return string.Empty;
        }

        var plain = FindPart(payload, "text/plain") is { } plainPart ? Normalize(Decode(plainPart)) : string.Empty;

        if (plain.Length >= MinUsefulPlainTextLength)
        {
            return plain;
        }

        var html = FindPart(payload, "text/html") is { } htmlPart ? HtmlToText(Decode(htmlPart)) : string.Empty;

        return html.Length > plain.Length ? html : plain;
    }

    public static string Header(GmailPayload? payload, string name) =>
        payload?.Headers?.FirstOrDefault(header => string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))?.Value
        ?? string.Empty;

    public static string SenderDomain(string from)
    {
        var match = EmailAddressPattern().Match(from);

        return match.Success ? match.Groups["domain"].Value.ToLowerInvariant() : string.Empty;
    }

    internal static string HtmlToText(string html)
    {
        var text = InvisibleBlocksPattern().Replace(html, " ");
        text = LineBreakTagsPattern().Replace(text, "\n");
        text = CellTagsPattern().Replace(text, " ");
        text = AnyTagPattern().Replace(text, " ");

        return Normalize(WebUtility.HtmlDecode(text));
    }

    private static GmailPayload? FindPart(GmailPayload part, string mimeType)
    {
        if (string.Equals(part.MimeType, mimeType, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(part.Filename)
            && !string.IsNullOrEmpty(part.Body?.Data))
        {
            return part;
        }

        foreach (var child in part.Parts ?? Enumerable.Empty<GmailPayload>())
        {
            if (FindPart(child, mimeType) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static string Decode(GmailPayload part)
    {
        var bytes = WebEncoders.Base64UrlDecode(part.Body!.Data!);

        return EncodingOf(Header(part, "Content-Type")).GetString(bytes);
    }

    private static Encoding EncodingOf(string contentType)
    {
        var charset = CharsetPattern().Match(contentType);

        if (!charset.Success)
        {
            return Encoding.UTF8;
        }

        try
        {
            return Encoding.GetEncoding(charset.Groups["charset"].Value);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    private static string Normalize(string text)
    {
        var lines = text
            .Replace(' ', ' ')
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => SpacesPattern().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);

        return string.Join('\n', lines);
    }

    [GeneratedRegex(@"<(head|style|script|title)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex InvisibleBlocksPattern();

    [GeneratedRegex(@"<\s*(br|/p|/div|/tr|/li|/h[1-6]|/table)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTagsPattern();

    [GeneratedRegex(@"<\s*/(td|th)\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex CellTagsPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex AnyTagPattern();

    [GeneratedRegex(@"[ \t\f\v]+")]
    private static partial Regex SpacesPattern();

    [GeneratedRegex(@"charset\s*=\s*""?(?<charset>[\w\-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CharsetPattern();

    [GeneratedRegex(@"[\w.+\-]+@(?<domain>[\w\-]+(\.[\w\-]+)+)")]
    private static partial Regex EmailAddressPattern();
}

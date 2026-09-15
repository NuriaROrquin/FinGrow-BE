namespace FinGrow.Infrastructure.Integrations.Twilio;

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using FinGrow.Application.Interfaces;
using Microsoft.Extensions.Options;

internal sealed class TwilioRequestValidator : ITwilioRequestValidator
{
    private readonly byte[] _authToken;

    public TwilioRequestValidator(IOptions<TwilioOptions> options) =>
        _authToken = Encoding.UTF8.GetBytes(options.Value.AuthToken);

    public bool IsValid(string url, IReadOnlyDictionary<string, string> form, string? signature)
    {
        if (string.IsNullOrEmpty(signature))
        {
            return false;
        }

        var expected = Sign(url, form);
        Span<byte> received = stackalloc byte[expected.Length];

        return Convert.TryFromBase64String(signature, received, out var written)
            && written == expected.Length
            && CryptographicOperations.FixedTimeEquals(expected, received);
    }

    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "El algoritmo lo fija Twilio: sus webhooks vienen firmados con HMAC-SHA1.")]
    private byte[] Sign(string url, IReadOnlyDictionary<string, string> form)
    {
        var payload = new StringBuilder(url);

        foreach (var (key, value) in form.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            payload.Append(key).Append(value);
        }

        return HMACSHA1.HashData(_authToken, Encoding.UTF8.GetBytes(payload.ToString()));
    }
}

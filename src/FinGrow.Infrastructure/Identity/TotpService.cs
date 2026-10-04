namespace FinGrow.Infrastructure.Identity;

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FinGrow.Application.Interfaces;

internal sealed class TotpService : ITotpService
{
    public const string Issuer = "FinGrow";

    private const int SecretBytes = 20;
    private const int Digits = 6;
    private const int StepSeconds = 30;

    private const int AllowedDriftSteps = 1;

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerateSecret() => ToBase32(RandomNumberGenerator.GetBytes(SecretBytes));

    public bool VerifyCode(string secret, string code, DateTimeOffset now)
    {
        var normalized = (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);

        if (normalized.Length != Digits || !normalized.All(char.IsAsciiDigit))
        {
            return false;
        }

        var key = FromBase32(secret);
        var currentStep = now.ToUnixTimeSeconds() / StepSeconds;
        var expected = Encoding.ASCII.GetBytes(normalized);
        var matched = false;

        for (var drift = -AllowedDriftSteps; drift <= AllowedDriftSteps; drift++)
        {
            var candidate = Encoding.ASCII.GetBytes(ComputeCode(key, currentStep + drift));
            matched |= CryptographicOperations.FixedTimeEquals(candidate, expected);
        }

        return matched;
    }

    public string BuildProvisioningUri(string secret, string accountName)
    {
        var label = Uri.EscapeDataString($"{Issuer}:{accountName}");
        return $"otpauth://totp/{label}?secret={secret}&issuer={Uri.EscapeDataString(Issuer)}&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
    }

    [SuppressMessage(
        "Security",
        "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "RFC 6238 y las apps de autenticacion exigen HMAC-SHA1; como HMAC no le afectan las colisiones de SHA-1.")]
    internal static string ComputeCode(byte[] key, long step)
    {
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);

        Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];
        HMACSHA1.HashData(key, counter, hash);

        var offset = hash[^1] & 0x0F;
        var binary = (BinaryPrimitives.ReadInt32BigEndian(hash.Slice(offset, 4)) & 0x7FFFFFFF) % 1_000_000;

        return binary.ToString("D6", CultureInfo.InvariantCulture);
    }

    internal static string ToBase32(ReadOnlySpan<byte> data)
    {
        var builder = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;

        foreach (var value in data)
        {
            buffer = ((buffer << 8) | value) & 0xFFFF;
            bits += 8;

            while (bits >= 5)
            {
                builder.Append(Base32Alphabet[(buffer >> (bits - 5)) & 0x1F]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            builder.Append(Base32Alphabet[(buffer << (5 - bits)) & 0x1F]);
        }

        return builder.ToString();
    }

    internal static byte[] FromBase32(string value)
    {
        var cleaned = value.TrimEnd('=').ToUpperInvariant();
        var output = new byte[cleaned.Length * 5 / 8];
        int buffer = 0, bits = 0, index = 0;

        foreach (var character in cleaned)
        {
            var position = Base32Alphabet.IndexOf(character, StringComparison.Ordinal);

            if (position < 0)
            {
                throw new FormatException("El secreto del doble factor no es base32 valido.");
            }

            buffer = ((buffer << 5) | position) & 0xFFFF;
            bits += 5;

            if (bits >= 8)
            {
                output[index++] = (byte)(buffer >> (bits - 8));
                bits -= 8;
            }
        }

        return output;
    }
}

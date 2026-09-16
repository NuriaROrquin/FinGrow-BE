namespace FinGrow.Infrastructure.Persistence.Protection;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmSecretProtector(IOptions<TokenEncryptionOptions> options)
        : this(Convert.FromBase64String(options.Value.Key))
    {
    }

    public AesGcmSecretProtector(byte[] key) => _key = key;

    public string Protect(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var output = new byte[NonceSize + bytes.Length + TagSize];
        var nonce = output.AsSpan(0, NonceSize);
        var ciphertext = output.AsSpan(NonceSize, bytes.Length);
        var tag = output.AsSpan(NonceSize + bytes.Length, TagSize);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, bytes, ciphertext, tag);

        return Convert.ToBase64String(output);
    }

    public string Unprotect(string ciphertext)
    {
        var input = Convert.FromBase64String(ciphertext);
        var nonce = input.AsSpan(0, NonceSize);
        var payload = input.AsSpan(NonceSize, input.Length - NonceSize - TagSize);
        var tag = input.AsSpan(input.Length - TagSize, TagSize);
        var plaintext = new byte[payload.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, payload, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}

namespace FinGrow.Infrastructure.Persistence.Protection;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

internal sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public const string Annotation = "FinGrow:Encrypted";

    public EncryptedStringConverter(ISecretProtector protector)
        : base(plaintext => protector.Protect(plaintext), ciphertext => protector.Unprotect(ciphertext))
    {
    }
}

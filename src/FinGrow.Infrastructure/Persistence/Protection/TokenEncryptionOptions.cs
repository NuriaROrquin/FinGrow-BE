namespace FinGrow.Infrastructure.Persistence.Protection;

using System.ComponentModel.DataAnnotations;

public sealed class TokenEncryptionOptions : IValidatableObject
{
    public const string SectionName = "TokenEncryption";
    public const int KeyBytes = 32;

    [Required(ErrorMessage = "Falta configurar la clave que cifra los tokens de las integraciones (TokenEncryption:Key).")]
    public string Key { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var buffer = new byte[KeyBytes];

        if (!Convert.TryFromBase64String(Key, buffer, out var written) || written != KeyBytes)
        {
            yield return new ValidationResult(
                $"TokenEncryption:Key tiene que ser {KeyBytes} bytes aleatorios en base64 (openssl rand -base64 {KeyBytes}).",
                new[] { nameof(Key) });
        }
    }
}

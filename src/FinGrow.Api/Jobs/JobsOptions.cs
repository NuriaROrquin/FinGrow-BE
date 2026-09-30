namespace FinGrow.Api.Jobs;

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

public sealed class JobsOptions
{
    public const string SectionName = "Jobs";
    public const int MinApiKeyLength = 16;

    [Required(ErrorMessage = "Falta configurar la clave con la que se disparan los trabajos programados (Jobs:ApiKey).")]
    [MinLength(MinApiKeyLength, ErrorMessage = "La clave de los trabajos programados (Jobs:ApiKey) tiene que tener al menos 16 caracteres.")]
    public string ApiKey { get; init; } = string.Empty;

    public bool Accepts(string? providedKey)
    {
        if (string.IsNullOrEmpty(providedKey))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(ApiKey),
            Encoding.UTF8.GetBytes(providedKey));
    }
}

namespace FinGrow.Infrastructure.Identity;

using System.ComponentModel.DataAnnotations;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Falta configurar la clave de firma del JWT (Jwt:SecretKey).")]
    [MinLength(32, ErrorMessage = "Jwt:SecretKey debe tener al menos 32 caracteres.")]
    public string SecretKey { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar el emisor del JWT (Jwt:Issuer).")]
    public string Issuer { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar la audiencia del JWT (Jwt:Audience).")]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Jwt:ExpirationMinutes debe estar entre 1 y 1440.")]
    public int ExpirationMinutes { get; init; } = 60;
}

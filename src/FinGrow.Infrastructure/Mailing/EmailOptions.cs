namespace FinGrow.Infrastructure.Mailing;

using System.ComponentModel.DataAnnotations;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required(ErrorMessage = "Falta configurar la API key de Resend (Email:ResendApiKey).")]
    public string ResendApiKey { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar el remitente de los correos (Email:From).")]
    public string From { get; init; } = string.Empty;

    public string? RedirectAllTo { get; init; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

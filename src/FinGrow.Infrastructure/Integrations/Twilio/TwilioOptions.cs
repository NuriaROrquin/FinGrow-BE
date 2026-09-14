namespace FinGrow.Infrastructure.Integrations.Twilio;

using System.ComponentModel.DataAnnotations;

public sealed class TwilioOptions
{
    public const string SectionName = "Twilio";

    [Required(ErrorMessage = "Falta configurar el Account SID de Twilio (Twilio:AccountSid).")]
    public string AccountSid { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar el Auth Token de Twilio (Twilio:AuthToken).")]
    public string AuthToken { get; init; } = string.Empty;

    public string? PublicBaseUrl { get; init; }

    [Range(1, 300)]
    public int MediaTimeoutSeconds { get; init; } = 30;
}

namespace FinGrow.Infrastructure.Integrations.Gmail;

using System.ComponentModel.DataAnnotations;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    [Required(ErrorMessage = "Falta configurar el Client ID del cliente OAuth de Google (Gmail:ClientId).")]
    public string ClientId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar el Client Secret del cliente OAuth de Google (Gmail:ClientSecret).")]
    public string ClientSecret { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar la URL de redireccion cargada en el cliente OAuth de Google (Gmail:RedirectUri).")]
    [Url]
    public string RedirectUri { get; init; } = string.Empty;

    [Url]
    public string AuthorizationUrl { get; init; } = "https://accounts.google.com/o/oauth2/v2/auth";

    [Url]
    public string OAuthBaseUrl { get; init; } = "https://oauth2.googleapis.com/";

    [Url]
    public string ApiBaseUrl { get; init; } = "https://gmail.googleapis.com/";

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

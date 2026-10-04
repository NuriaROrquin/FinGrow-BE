namespace FinGrow.Infrastructure.Integrations.ArgentinaDatos;

using System.ComponentModel.DataAnnotations;

public sealed class ArgentinaDatosOptions
{
    public const string SectionName = "ArgentinaDatos";

    public const string HttpClientName = "ArgentinaDatos";

    [Required(ErrorMessage = "Falta configurar la URL base de ArgentinaDatos (ArgentinaDatos:BaseUrl).")]
    [Url]
    public string BaseUrl { get; init; } = "https://api.argentinadatos.com/";

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 15;

    [Range(0, 1440)]
    public int CacheMinutes { get; init; } = 60;

    [Range(1, 365)]
    public int StaleAfterDays { get; init; } = 30;
}

namespace FinGrow.Infrastructure.Integrations.DolarApi;

using System.ComponentModel.DataAnnotations;

public sealed class DolarApiOptions
{
    public const string SectionName = "DolarApi";

    [Required(ErrorMessage = "Falta configurar la URL base de DolarApi (DolarApi:BaseUrl).")]
    [Url]
    public string BaseUrl { get; init; } = "https://dolarapi.com/";

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;

    [Range(0, 1440)]
    public int CacheMinutes { get; init; } = 5;
}

namespace FinGrow.Infrastructure.Integrations.Byma;

using System.ComponentModel.DataAnnotations;

public sealed class BymaOptions
{
    public const string SectionName = "Byma";

    public const string HttpClientName = "Byma";

    [Required(ErrorMessage = "Falta configurar la URL base de los datos de BYMA (Byma:BaseUrl).")]
    [Url]
    public string BaseUrl { get; init; } = "https://open.bymadata.com.ar/vanoms-be-core/rest/api/bymadata/free/";

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    [Range(1, 100)]
    public int MaxPagesPerPanel { get; init; } = 20;

    [Range(0, 1440)]
    public int CacheMinutes { get; init; } = 5;
}

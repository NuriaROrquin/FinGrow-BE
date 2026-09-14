namespace FinGrow.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    // Es el header que valida FinGrow-AI (app/api/security.py); cambiarlo rompe el contrato.
    public const string ApiKeyHeader = "X-API-Key";

    [Required(ErrorMessage = "Falta configurar la URL base del servicio de IA (AiService:BaseUrl).")]
    public string BaseUrl { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar la API key del servicio de IA (AiService:ApiKey).")]
    public string ApiKey { get; init; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

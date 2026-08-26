namespace FinGrow.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    [Required(ErrorMessage = "Falta configurar la URL base del servicio de IA (AiService:BaseUrl).")]
    public string BaseUrl { get; init; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

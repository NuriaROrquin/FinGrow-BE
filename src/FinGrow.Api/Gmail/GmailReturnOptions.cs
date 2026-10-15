namespace FinGrow.Api.Gmail;

using System.ComponentModel.DataAnnotations;

public sealed class GmailReturnOptions
{
    public const string SectionName = "Gmail";

    [Required(ErrorMessage = "Falta configurar a donde vuelve el navegador al terminar la vinculacion (Gmail:FrontendReturnUrl).")]
    [Url]
    public string FrontendReturnUrl { get; init; } = string.Empty;
}

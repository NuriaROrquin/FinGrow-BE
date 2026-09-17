namespace FinGrow.Api.MercadoPago;

using System.ComponentModel.DataAnnotations;

public sealed class MercadoPagoReturnOptions
{
    public const string SectionName = "MercadoPago";

    [Required(ErrorMessage = "Falta configurar a donde vuelve el navegador al terminar la vinculacion (MercadoPago:FrontendReturnUrl).")]
    [Url]
    public string FrontendReturnUrl { get; init; } = string.Empty;
}

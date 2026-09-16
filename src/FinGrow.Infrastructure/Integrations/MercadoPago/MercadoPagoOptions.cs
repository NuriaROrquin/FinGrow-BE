namespace FinGrow.Infrastructure.Integrations.MercadoPago;

using System.ComponentModel.DataAnnotations;

public sealed class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    [Required(ErrorMessage = "Falta configurar el Client ID de la aplicacion de Mercado Pago (MercadoPago:ClientId).")]
    public string ClientId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar el Client Secret de la aplicacion de Mercado Pago (MercadoPago:ClientSecret).")]
    public string ClientSecret { get; init; } = string.Empty;

    [Required(ErrorMessage = "Falta configurar la URL de redireccion cargada en la aplicacion de Mercado Pago (MercadoPago:RedirectUri).")]
    [Url]
    public string RedirectUri { get; init; } = string.Empty;

    [Url]
    public string AuthorizationUrl { get; init; } = "https://auth.mercadopago.com.ar/authorization";

    [Url]
    public string ApiBaseUrl { get; init; } = "https://api.mercadopago.com/";

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    [Range(1, 1440)]
    public int SyncIntervalMinutes { get; init; } = 60;

    [Range(0, 3600)]
    public int SyncInitialDelaySeconds { get; init; } = 30;
}

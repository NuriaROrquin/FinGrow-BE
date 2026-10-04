namespace FinGrow.Application.Features.SecurityPrices;

using Common;
using Domain.Enums;

internal static class SecurityPriceErrors
{
    public static Error NotFound(string symbol, Currency currency) =>
        Error.NotFound(
            "SecurityPrice.NotFound",
            $"BYMA no tiene cotizacion de {symbol} en {currency}. Revisa el simbolo o usa la variante de la moneda: AL30 en pesos, AL30D en dolares.");

    public static Error Unavailable() =>
        Error.Unavailable(
            "SecurityPrice.Unavailable",
            "No pudimos obtener la cotizacion de BYMA. Proba de nuevo en unos minutos.");
}

namespace FinGrow.Application.Features.ExchangeRates;

using Common;

internal static class ExchangeRateErrors
{
    public static Error MepUnavailable() =>
        Error.Unavailable(
            "ExchangeRate.MepUnavailable",
            "No pudimos obtener la cotizacion del dolar MEP. Proba de nuevo en unos minutos.");
}

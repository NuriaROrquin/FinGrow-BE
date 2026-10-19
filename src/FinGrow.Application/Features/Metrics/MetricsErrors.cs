namespace FinGrow.Application.Features.Metrics;

using FinGrow.Application.Common;

internal static class MetricsErrors
{
    public static readonly Error NoAutenticado =
        Error.Unauthorized("Metrics.NoAutenticado", "Hay que iniciar sesión como empresa para ver las métricas.");

    public static readonly Error NotEnoughPeriods =
        Error.Validation(
            "Metrics.NotEnoughPeriods",
            "La comparacion necesita al menos dos periodos con datos.");
}

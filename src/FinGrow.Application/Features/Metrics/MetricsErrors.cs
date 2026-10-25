namespace FinGrow.Application.Features.Metrics;

using FinGrow.Application.Common;

internal static class MetricsErrors
{
    public static readonly Error NoAutenticado =
        Error.Unauthorized("Metrics.NoAutenticado", "Hay que iniciar sesión como empresa para ver las métricas.");

}

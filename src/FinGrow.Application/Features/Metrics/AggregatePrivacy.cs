namespace FinGrow.Application.Features.Metrics;

internal static class AggregatePrivacy
{
    // Con menos gente que esto, una tasa deja deducir qué hizo cada uno (en un grupo de 1 la tasa
    // es directamente la respuesta). Es el piso hasta que T-11 traiga la capa de anonimización.
    public const int MinimumGroupSize = 5;

    public static bool CanDisclose(int groupSize) => groupSize >= MinimumGroupSize;
}

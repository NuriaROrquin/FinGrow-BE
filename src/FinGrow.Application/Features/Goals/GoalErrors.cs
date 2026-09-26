namespace FinGrow.Application.Features.Goals;

using Common;

internal static class GoalErrors
{
    public static Error NotFound(Guid goalId) =>
        Error.NotFound("Goal.NotFound", $"No existe una meta con id '{goalId}'.");

    public static Error ContributionNotFound(Guid contributionId) =>
        Error.NotFound("Goal.ContributionNotFound", $"No existe un aporte con id '{contributionId}' en esta meta.");

    public static Error NotActive() =>
        Error.Conflict("Goal.NotActive", "Solo se puede registrar un aporte en una meta activa.");

    public static Error Cancelled() =>
        Error.Conflict("Goal.Cancelled", "Una meta cancelada no se puede editar.");

    public static Error CurrencyMismatch() =>
        Error.Validation(
            "Goal.CurrencyMismatch",
            "El aporte tiene que estar en la misma moneda que el objetivo de la meta.");
}

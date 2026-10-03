namespace FinGrow.Application.Features.Budgets;

using System.Globalization;
using Common;

internal static class BudgetErrors
{
    public static Error AlreadyExists(DateOnly periodStart) =>
        Error.Conflict(
            "Budget.AlreadyExists",
            string.Create(
                CultureInfo.InvariantCulture,
                $"Ya existe un presupuesto para {periodStart:MM/yyyy}. Editalo en lugar de crear otro."));

    public static Error NotFound(DateOnly periodStart) =>
        Error.NotFound(
            "Budget.NotFound",
            string.Create(
                CultureInfo.InvariantCulture,
                $"No tenés un presupuesto para {periodStart:MM/yyyy}."));

    public static Error PreviousNotFound(DateOnly previousPeriodStart) =>
        Error.NotFound(
            "Budget.PreviousNotFound",
            string.Create(
                CultureInfo.InvariantCulture,
                $"No hay un presupuesto de {previousPeriodStart:MM/yyyy} para duplicar."));

    public static Error LimitNotFound() =>
        Error.NotFound("Budget.LimitNotFound", "El presupuesto no tiene un tope para esa categoría.");

    public static Error LastLimit() =>
        Error.Conflict(
            "Budget.LastLimit",
            "Es la única categoría del presupuesto. Si no lo querés, eliminá el presupuesto completo.");

}

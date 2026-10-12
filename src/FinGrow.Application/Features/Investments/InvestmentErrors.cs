namespace FinGrow.Application.Features.Investments;

using Common;

internal static class InvestmentErrors
{
    public static Error NotFound(Guid investmentId) =>
        Error.NotFound("Investment.NotFound", $"No existe una inversion con id '{investmentId}'.");

    public static Error NotOwned() =>
        Error.Forbidden("Investment.NotOwned", "La inversion no pertenece al usuario autenticado.");
}

namespace FinGrow.Application.Features.Session;

using FinGrow.Application.Common;

internal static class SessionErrors
{
    public static readonly Error Unauthenticated =
        Error.Forbidden("Session.Unauthenticated", "Hay que iniciar sesion para administrar las sesiones.");

    public static readonly Error NotFound =
        Error.NotFound("Session.NotFound", "La sesion no existe, ya vencio o ya fue cerrada.");

    public static readonly Error NotOwned =
        Error.Forbidden("Session.NotOwned", "La sesion es de otro usuario.");

    public static readonly Error IsCurrent =
        Error.Validation("Session.IsCurrent", "Para cerrar la sesion actual, usa cerrar sesion.");

    public static readonly Error CurrentUnknown =
        Error.Conflict("Session.CurrentUnknown", "No se pudo identificar la sesion actual. Volve a iniciar sesion.");
}

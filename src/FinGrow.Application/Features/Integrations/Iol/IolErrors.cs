namespace FinGrow.Application.Features.Integrations.Iol;

using System.Net;
using FinGrow.Application.Common;

internal static class IolErrors
{
    public static readonly Error InvalidCredentials = Error.Validation(
        "Integrations.Iol.InvalidCredentials", "IOL rechazo el usuario o la contraseña. Revisalos y verifica que tengas las APIs activadas en tu cuenta de IOL (menu de usuario > Configuracion API).");

    public static readonly Error AlreadyLinkedToAnotherEmployee = Error.Conflict(
        "Integrations.Iol.AccountInUse", "Esa cuenta de IOL ya esta vinculada a otro empleado.");

    public static readonly Error Unavailable = Error.Unavailable(
        "Integrations.Iol.Unavailable", "IOL no responde en este momento. Volve a intentar en unos minutos.");

    public static readonly Error NotLinked = Error.NotFound(
        "Integrations.Iol.NotLinked", "No tenes IOL vinculado.");

    public static readonly Error GrantExpired = Error.Failure(
        "Integrations.Iol.GrantExpired", "La autorizacion de IOL vencio o fue revocada: volve a vincular la cuenta.");

    public static bool IsRejection(HttpRequestException exception) =>
        exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized;
}

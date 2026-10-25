namespace FinGrow.Api.Authentication;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Atiende el request como anónimo aunque traiga la cookie de sesión. Es para los endpoints que
/// identifican a la cuenta por otro medio (el email o el enlace de HU-72): una sesión abierta en
/// el mismo navegador, de otra cuenta y hasta de otra empresa, activaría el filtro de empresa de
/// T-10 y escondería las filas que el caso de uso necesita.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IgnoreSessionAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}

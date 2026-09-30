namespace FinGrow.Api.Jobs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidateJobsKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string KeyHeader = "X-Jobs-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<JobsOptions>>().Value;
        var providedKey = context.HttpContext.Request.Headers[KeyHeader].ToString();

        if (options.Accepts(providedKey))
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Clave de trabajos invalida.",
            Detail = $"El request no trae en la cabecera {KeyHeader} la clave configurada en Jobs:ApiKey.",
        })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}

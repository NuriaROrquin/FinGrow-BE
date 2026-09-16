namespace FinGrow.Api.Telegram;

using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidateTelegramSecretAttribute : Attribute, IAuthorizationFilter
{
    public const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var validator = context.HttpContext.RequestServices.GetRequiredService<ITelegramWebhookValidator>();
        var secret = context.HttpContext.Request.Headers[SecretHeader].ToString();

        if (validator.IsValid(secret))
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Secreto de Telegram invalido.",
            Detail = "El request no trae el secreto configurado al registrar el webhook.",
        })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}

namespace FinGrow.Api.Twilio;

using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidateTwilioSignatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string SignatureHeader = "X-Twilio-Signature";
    public const string PublicBaseUrlKey = "Twilio:PublicBaseUrl";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.HttpContext.RequestServices;
        var validator = request.GetRequiredService<ITwilioRequestValidator>();
        var configuration = request.GetRequiredService<IConfiguration>();

        var form = context.HttpContext.Request.HasFormContentType
            ? await context.HttpContext.Request.ReadFormAsync(context.HttpContext.RequestAborted)
            : null;

        var fields = form?.ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.Ordinal)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);

        var url = ResolveUrl(context.HttpContext.Request, configuration[PublicBaseUrlKey]);
        var signature = context.HttpContext.Request.Headers[SignatureHeader].ToString();

        if (!validator.IsValid(url, fields, signature))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Firma de Twilio invalida.",
                Detail = "El request no viene firmado con el Auth Token de la cuenta configurada.",
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }

    private static string ResolveUrl(HttpRequest request, string? publicBaseUrl) =>
        string.IsNullOrWhiteSpace(publicBaseUrl)
            ? request.GetDisplayUrl()
            : $"{publicBaseUrl.TrimEnd('/')}{request.PathBase}{request.Path}{request.QueryString}";
}

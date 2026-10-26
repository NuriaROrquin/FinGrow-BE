namespace FinGrow.Api.Authentication;

using System.Security.Claims;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

public static class SessionCookie
{
    public const string Name = "fingrow-session";

    public const string RefreshName = "fingrow-refresh";

    private const string RefreshPath = "/session";

    public static void Append(HttpResponse response, string token, DateTimeOffset expiresAt) =>
        response.Cookies.Append(Name, token, BuildOptions(expiresAt, "/"));

    public static void AppendRefresh(HttpResponse response, string token, DateTimeOffset expiresAt) =>
        response.Cookies.Append(RefreshName, token, BuildOptions(expiresAt, RefreshPath));

    public static void Delete(HttpResponse response)
    {
        response.Cookies.Delete(Name, BuildOptions(expiresAt: null, "/"));
        response.Cookies.Delete(RefreshName, BuildOptions(expiresAt: null, RefreshPath));
    }

    public static IServiceCollection AddSessionCookieAuthentication(this IServiceCollection services)
    {
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token)
                        && !context.Request.Headers.ContainsKey("Authorization")
                        && context.Request.Cookies.TryGetValue(Name, out var cookieToken))
                    {
                        context.Token = cookieToken;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = RejectClosedSessionAsync,
            });

        return services;
    }

    // Cerrar una sesion revoca su refresh token; sin este chequeo el access token seguiria valiendo
    // hasta vencer (HU-73). Un token sin session_id es de antes de que el access token llevara su
    // sesion y vence solo en a lo sumo Jwt:ExpirationMinutes: rechazarlo dejaria afuera a todos al
    // desplegar.
    private static async Task RejectClosedSessionAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;

        if (!Guid.TryParse(principal?.FindFirstValue(SessionClaims.SessionId), out var sessionId))
        {
            return;
        }

        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            context.Fail("El token no identifica al usuario.");
            return;
        }

        var sessions = context.HttpContext.RequestServices.GetRequiredService<IOpenSessionChecker>();

        if (!await sessions.IsOpenAsync(userId, sessionId, context.HttpContext.RequestAborted))
        {
            context.Fail("La sesion fue cerrada.");
        }
    }

    private static CookieOptions BuildOptions(DateTimeOffset? expiresAt, string path) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = path,
        Expires = expiresAt,
    };
}

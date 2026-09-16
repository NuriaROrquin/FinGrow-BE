namespace FinGrow.Api.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;

public static class SessionCookie
{
    public const string Name = "fingrow-session";

    public static void Append(HttpResponse response, string token, DateTimeOffset expiresAt) =>
        response.Cookies.Append(Name, token, BuildOptions(expiresAt));

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, BuildOptions(expiresAt: null));

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
            });

        return services;
    }

    private static CookieOptions BuildOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = "/",
        Expires = expiresAt,
    };
}

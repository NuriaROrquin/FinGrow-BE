namespace FinGrow.Api.UnitTests.Authentication;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinGrow.Api.Contracts;
using FinGrow.Application.Interfaces;
using FinGrow.Api.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class JwtAuthenticationTests : IClassFixture<JwtAuthenticationTests.SecureEndpointWebApplicationFactory>
{
    private readonly SecureEndpointWebApplicationFactory _factory;

    public JwtAuthenticationTests(SecureEndpointWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Secure_endpoint_returns_401_without_a_token()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/test/secure", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Secure_endpoint_returns_401_with_a_malformed_token()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await client.GetAsync(new Uri("/test/secure", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Secure_endpoint_returns_200_and_resolves_the_claims_with_a_valid_token()
    {
        var userId = Guid.CreateVersion7();
        var companyId = Guid.CreateVersion7();
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var token = tokenService.GenerateToken(userId, companyId, "Employee", "Ana Pérez");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        var response = await client.GetAsync(new Uri("/test/secure", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecureEndpointResponse>();
        body.ShouldNotBeNull();
        body.UserId.ShouldBe(userId);
        body.CompanyId.ShouldBe(companyId);
    }

    [Fact]
    public async Task Secure_endpoint_returns_200_with_a_valid_token_in_the_session_cookie()
    {
        var userId = Guid.CreateVersion7();
        var companyId = Guid.CreateVersion7();
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var token = tokenService.GenerateToken(userId, companyId, "Employee", "Ana Pérez");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{SessionCookie.Name}={token.Value}");

        var response = await client.GetAsync(new Uri("/test/secure", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecureEndpointResponse>();
        body.ShouldNotBeNull();
        body.UserId.ShouldBe(userId);
        body.CompanyId.ShouldBe(companyId);
    }

    [Fact]
    public async Task Session_endpoint_returns_the_claims_and_delete_clears_the_cookie()
    {
        var userId = Guid.CreateVersion7();
        var companyId = Guid.CreateVersion7();
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var token = tokenService.GenerateToken(userId, companyId, "Empleado", "Ana Pérez");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{SessionCookie.Name}={token.Value}");

        var session = await client.GetFromJsonAsync<SessionResponse>(new Uri("/session", UriKind.Relative));

        session.ShouldNotBeNull();
        session.UserId.ShouldBe(userId);
        session.CompanyId.ShouldBe(companyId);
        session.FullName.ShouldBe("Ana Pérez");
        session.Role.ShouldBe("Empleado");
        session.ExpiresAt.ShouldBe(token.ExpiresAt, TimeSpan.FromSeconds(1));

        var logout = await client.DeleteAsync(new Uri("/session", UriKind.Relative));

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var setCookies = logout.Headers.GetValues("Set-Cookie").ToList();
        setCookies.Count.ShouldBe(2);

        var sessionCookie = setCookies.Single(cookie => cookie.StartsWith($"{SessionCookie.Name}=;", StringComparison.Ordinal));
        sessionCookie.ShouldContain("httponly", Case.Insensitive);

        var refreshCookie = setCookies.Single(cookie => cookie.StartsWith($"{SessionCookie.RefreshName}=;", StringComparison.Ordinal));
        refreshCookie.ShouldContain("httponly", Case.Insensitive);
    }

    private sealed record SecureEndpointResponse(Guid? UserId, Guid? CompanyId);

    public sealed class SecureEndpointWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
                    ["AiService:ApiKey"] = "unit-test-ai-api-key",
                    ["Twilio:AccountSid"] = "ACunit-test",
                    ["Twilio:AuthToken"] = "unit-test-twilio-auth-token",
                    ["Telegram:BotToken"] = "unit-test-telegram-bot-token",
                    ["Telegram:WebhookSecret"] = "unit-test-telegram-secret",
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                }));

            builder.ConfigureTestServices(services =>
                services.AddSingleton<IStartupFilter, SecureTestEndpointStartupFilter>());
        }
    }

    private sealed class SecureTestEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);

            app.UseEndpoints(endpoints =>
                endpoints.MapGet("/test/secure", (ICurrentUser currentUser) =>
                        Results.Ok(new SecureEndpointResponse(currentUser.UserId, currentUser.CompanyId)))
                    .RequireAuthorization());
        };
    }
}

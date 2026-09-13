namespace FinGrow.Api.UnitTests.Authentication;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinGrow.Application.Interfaces;
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
        var token = tokenService.GenerateToken(userId, companyId, "Employee");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(new Uri("/test/secure", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecureEndpointResponse>();
        body.ShouldNotBeNull();
        body.UserId.ShouldBe(userId);
        body.CompanyId.ShouldBe(companyId);
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

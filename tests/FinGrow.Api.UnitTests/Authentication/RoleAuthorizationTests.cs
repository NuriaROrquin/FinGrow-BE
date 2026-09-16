namespace FinGrow.Api.UnitTests.Authentication;

using System.Net;
using System.Net.Http.Headers;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class RoleAuthorizationTests : IClassFixture<RoleAuthorizationTests.RoleEndpointsWebApplicationFactory>
{
    private readonly RoleEndpointsWebApplicationFactory _factory;

    public RoleAuthorizationTests(RoleEndpointsWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task A_company_token_is_rejected_with_403_on_an_employee_endpoint()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await CreateTokenAsync(Rol.Empresa));

        var response = await client.GetAsync(new Uri("/test/empleado", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_employee_token_is_rejected_with_403_on_a_company_endpoint()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await CreateTokenAsync(Rol.Empleado));

        var response = await client.GetAsync(new Uri("/test/empresa", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_employee_token_is_accepted_on_an_employee_endpoint()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await CreateTokenAsync(Rol.Empleado));

        var response = await client.GetAsync(new Uri("/test/empleado", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_company_token_is_accepted_on_a_company_endpoint()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await CreateTokenAsync(Rol.Empresa));

        var response = await client.GetAsync(new Uri("/test/empresa", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<string> CreateTokenAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var token = tokenService.GenerateToken(Guid.CreateVersion7(), Guid.CreateVersion7(), role, "Test");

        return await Task.FromResult(token.Value);
    }

    public sealed class RoleEndpointsWebApplicationFactory : WebApplicationFactory<Program>
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
                }));

            builder.ConfigureTestServices(services =>
                services.AddSingleton<IStartupFilter, RoleTestEndpointsStartupFilter>());
        }
    }

    private sealed class RoleTestEndpointsStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/test/empleado", () => Results.Ok())
                    .RequireAuthorization(policy => policy.RequireRole(Rol.Empleado));

                endpoints.MapGet("/test/empresa", () => Results.Ok())
                    .RequireAuthorization(policy => policy.RequireRole(Rol.Empresa));
            });
        };
    }
}

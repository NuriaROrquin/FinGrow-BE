namespace FinGrow.Api.UnitTests.Authentication;

using System.Net;
using System.Net.Http.Json;
using FinGrow.Api.Authentication;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class SessionManagementEndpointsTests : IClassFixture<SessionManagementEndpointsTests.SessionsWebApplicationFactory>
{
    private readonly SessionsWebApplicationFactory _factory;

    public SessionManagementEndpointsTests(SessionsWebApplicationFactory factory) => _factory = factory;

    private (RefreshToken Token, string Value) OpenSession(Guid userId, Guid companyId, string? userAgent)
    {
        var value = RefreshToken.GenerateValue();
        var token = RefreshToken.Issue(userId, companyId, "Empleado", value, DateTimeOffset.UtcNow.AddMinutes(-5), userAgent);
        _factory.Tokens.Add(token);
        return (token, value);
    }

    private HttpClient CreateClient(Guid userId, Guid companyId, string? refreshToken)
    {
        using var scope = _factory.Services.CreateScope();
        var accessToken = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(userId, companyId, "Empleado", "Ana Pérez");

        var client = _factory.CreateClient();
        var cookie = $"{SessionCookie.Name}={accessToken.Value}";
        if (refreshToken is not null)
        {
            cookie += $"; {SessionCookie.RefreshName}={refreshToken}";
        }

        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client;
    }

    [Fact]
    public async Task Listing_sessions_requires_being_logged_in()
    {
        var response = await _factory.CreateClient().GetAsync(new Uri("/session/all", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_list_marks_the_session_of_the_refresh_cookie_as_current()
    {
        var userId = Guid.CreateVersion7();
        var companyId = Guid.CreateVersion7();
        var (current, currentValue) = OpenSession(userId, companyId, "Mozilla/5.0 (Windows NT 10.0) Chrome/141.0 Safari/537.36");
        var (other, _) = OpenSession(userId, companyId, userAgent: null);

        var sessions = await CreateClient(userId, companyId, currentValue)
            .GetFromJsonAsync<List<SessionRow>>(new Uri("/session/all", UriKind.Relative));

        sessions.ShouldNotBeNull();
        sessions.Count.ShouldBe(2);
        sessions[0].ShouldBe(new SessionRow(current.SessionId, "Chrome", "Windows", IsCurrent: true));
        sessions[1].ShouldBe(new SessionRow(other.SessionId, null, null, IsCurrent: false));
    }

    [Fact]
    public async Task Closing_one_session_and_then_the_others_leaves_only_the_current_one_open()
    {
        var userId = Guid.CreateVersion7();
        var companyId = Guid.CreateVersion7();
        var (current, currentValue) = OpenSession(userId, companyId, userAgent: null);
        var (phone, _) = OpenSession(userId, companyId, userAgent: null);
        var (tablet, _) = OpenSession(userId, companyId, userAgent: null);
        var client = CreateClient(userId, companyId, currentValue);

        var closeOne = await client.DeleteAsync(new Uri($"/session/{phone.SessionId}", UriKind.Relative));
        closeOne.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        phone.RevokedAt.ShouldNotBeNull();
        tablet.RevokedAt.ShouldBeNull();

        var closeCurrent = await client.DeleteAsync(new Uri($"/session/{current.SessionId}", UriKind.Relative));
        closeCurrent.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var closeOthers = await client.DeleteAsync(new Uri("/session/others", UriKind.Relative));
        closeOthers.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        tablet.RevokedAt.ShouldNotBeNull();
        current.RevokedAt.ShouldBeNull();
        closeOthers.Headers.Contains("Set-Cookie").ShouldBeFalse();
    }

    [Fact]
    public async Task Closing_the_others_without_a_refresh_cookie_closes_nothing()
    {
        var userId = Guid.CreateVersion7();
        var companyId = Guid.CreateVersion7();
        var (phone, _) = OpenSession(userId, companyId, userAgent: null);

        var response = await CreateClient(userId, companyId, refreshToken: null)
            .DeleteAsync(new Uri("/session/others", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        phone.RevokedAt.ShouldBeNull();
    }

    private sealed record SessionRow(Guid Id, string? Browser, string? OperatingSystem, bool IsCurrent);

    public sealed class SessionsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public List<RefreshToken> Tokens { get; } = new();

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
                    ["Email:ResendApiKey"] = "re_unit_test_key",
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["Gmail:ClientId"] = "unit-test-gmail-client-id",
                    ["Gmail:ClientSecret"] = "unit-test-gmail-client-secret",
                    ["Gmail:RedirectUri"] = "https://api.test/api/integrations/gmail/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key-1234",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IRefreshTokenRepository>(new InMemoryRefreshTokenRepository(Tokens));
                services.AddSingleton<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class InMemoryRefreshTokenRepository(List<RefreshToken> tokens) : IRefreshTokenRepository
    {
        public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

        public void Add(RefreshToken refreshToken) => tokens.Add(refreshToken);

        public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RefreshToken>>(tokens
                .Where(token => token.UserId == userId && token.IsActive(now))
                .OrderByDescending(token => token.CreatedAt)
                .ToList());

        public Task<RefreshToken?> FindActiveBySessionAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(tokens.FirstOrDefault(token => token.SessionId == sessionId && token.IsActive(now)));

        public Task RevokeAllForUserExceptSessionAsync(
            Guid userId,
            Guid keptSessionId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken = default)
        {
            foreach (var token in tokens.Where(t => t.UserId == userId && t.SessionId != keptSessionId))
            {
                token.Revoke(revokedAt);
            }

            return Task.CompletedTask;
        }
    }
}

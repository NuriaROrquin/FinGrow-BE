namespace FinGrow.Api.UnitTests.PasswordReset;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Quien abre el enlace puede tener abierta en ese navegador la sesión de otra cuenta, incluso de
/// otra empresa. El filtro de empresa de T-10 se aplica a toda consulta con sesión: si el flujo la
/// viera, los refresh tokens de la cuenta quedarían ocultos y restablecer la contraseña no le
/// cerraría las sesiones abiertas.
/// </summary>
public class PasswordResetEndpointsTests : IClassFixture<PasswordResetEndpointsTests.Factory>
{
    private const string TokenValue = "enlace-de-prueba";

    private readonly Factory _factory;

    public PasswordResetEndpointsTests(Factory factory)
    {
        _factory = factory;
        _factory.SessionsSeen.Clear();
        _factory.RevokedUsers.Clear();
    }

    [Theory]
    [InlineData("/password-reset", """{"email":"ana@otra.com"}""")]
    [InlineData("/password-reset/validate", """{"token":"enlace-de-prueba"}""")]
    [InlineData("/password-reset/confirm", """{"token":"enlace-de-prueba","newPassword":"nueva-clave-1234"}""")]
    public async Task The_flow_ignores_a_session_open_in_the_same_browser(string path, string body)
    {
        var client = _factory.CreateClientWithSessionOfAnotherCompany();

        await client.PostAsync(new Uri(path, UriKind.Relative), new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        _factory.SessionsSeen.ShouldNotBeEmpty();
        _factory.SessionsSeen.ShouldAllBe(isAuthenticated => !isAuthenticated);
    }

    [Fact]
    public async Task Resetting_with_a_session_of_another_company_open_still_closes_the_account_sessions()
    {
        var response = await _factory.CreateClientWithSessionOfAnotherCompany()
            .PostAsJsonAsync(
                new Uri("/password-reset/confirm", UriKind.Relative),
                new { token = TokenValue, newPassword = "nueva-clave-1234" });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _factory.RevokedUsers.ShouldBe(new[] { (_factory.Account.Id, false) });
    }

    [Fact]
    public async Task Requesting_a_link_answers_no_content_even_with_a_session_open()
    {
        var response = await _factory.CreateClientWithSessionOfAnotherCompany()
            .PostAsJsonAsync(new Uri("/password-reset", UriKind.Relative), new { email = "ana@otra.com" });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public Factory() =>
            Account = Employee.Create(
                Guid.NewGuid(),
                departmentId: null,
                "Ana Gómez",
                Email.From("ana@otra.com"),
                phoneNumber: null,
                "hash",
                Currency.ARS,
                new DateOnly(2026, 1, 15),
                DateTimeOffset.UtcNow.AddMonths(-1)).User;

        public User Account { get; }

        public List<bool> SessionsSeen { get; } = new();

        /// <summary>A quién se le revocaron las sesiones, y si en ese momento había sesión en el pedido.</summary>
        public List<(Guid UserId, bool WithSession)> RevokedUsers { get; } = new();

        public HttpClient CreateClientWithSessionOfAnotherCompany()
        {
            var client = CreateClient();

            using var scope = Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
                .GenerateToken(Guid.NewGuid(), Guid.NewGuid(), Rol.Empresa, "Otra S.A.");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            return client;
        }

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
                    ["Gmail:ClientId"] = "unit-test-gmail-client-id",
                    ["Gmail:ClientSecret"] = "unit-test-gmail-client-secret",
                    ["Gmail:RedirectUri"] = "https://api.test/api/integrations/gmail/oauth/callback",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IPasswordResetTokenRepository>(provider =>
                    new SessionRecordingTokenRepository(provider.GetRequiredService<ICurrentUser>(), SessionsSeen, Account));
                services.AddScoped<IUserRepository>(provider =>
                    new SessionRecordingUserRepository(provider.GetRequiredService<ICurrentUser>(), SessionsSeen, Account));
                services.AddScoped<IRefreshTokenRepository>(provider =>
                    new RecordingRefreshTokenRepository(provider.GetRequiredService<ICurrentUser>(), RevokedUsers));
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    // Anota si había sesión en el momento de cada consulta: es lo que decide si EF aplica el filtro de empresa.
    private sealed class SessionRecordingTokenRepository(ICurrentUser currentUser, List<bool> sessionsSeen, User account)
        : IPasswordResetTokenRepository
    {
        public Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            sessionsSeen.Add(currentUser.IsAuthenticated);
            return Task.FromResult(tokenHash == PasswordResetToken.Hash(TokenValue)
                ? PasswordResetToken.Issue(account.Id, TokenValue, DateTimeOffset.UtcNow)
                : null);
        }

        public Task<bool> AnyIssuedAfterAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default)
        {
            sessionsSeen.Add(currentUser.IsAuthenticated);
            return Task.FromResult(false);
        }

        public void Add(PasswordResetToken token) => sessionsSeen.Add(currentUser.IsAuthenticated);

        public Task InvalidateAllForUserAsync(Guid userId, DateTimeOffset usedAt, CancellationToken cancellationToken = default)
        {
            sessionsSeen.Add(currentUser.IsAuthenticated);
            return Task.CompletedTask;
        }
    }

    // Por email no encuentra a nadie, así el pedido de enlace corta antes de mandar un correo.
    private sealed class SessionRecordingUserRepository(ICurrentUser currentUser, List<bool> sessionsSeen, User account)
        : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            sessionsSeen.Add(currentUser.IsAuthenticated);
            return Task.FromResult(id == account.Id ? account : null);
        }

        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            sessionsSeen.Add(currentUser.IsAuthenticated);
            return Task.FromResult<User?>(null);
        }

        public void Remove(User user)
        {
        }
    }

    private sealed class RecordingRefreshTokenRepository(ICurrentUser currentUser, List<(Guid, bool)> revokedUsers)
        : IRefreshTokenRepository
    {
        public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshToken?>(null);

        public void Add(RefreshToken refreshToken)
        {
        }

        public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
        {
            revokedUsers.Add((userId, currentUser.IsAuthenticated));
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}

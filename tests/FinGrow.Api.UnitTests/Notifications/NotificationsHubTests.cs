namespace FinGrow.Api.UnitTests.Notifications;

using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using FinGrow.Api.Authentication;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class NotificationsHubTests
{
    private const string HubPath = "/hubs/notifications";
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Without_a_session_the_hub_rejects_the_connection()
    {
        using var factory = new HubWebApplicationFactory();
        await using var connection = BuildConnection(factory, sessionToken: null);

        var exception = await Should.ThrowAsync<HttpRequestException>(() => connection.StartAsync());

        exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_new_notification_reaches_the_connected_employee_with_its_type_as_text()
    {
        using var factory = new HubWebApplicationFactory();
        await using var employee = await Listener.ConnectAsync(factory, Guid.CreateVersion7(), Rol.Empleado);

        var notification = await SendAsync(factory, NotificationRecipient.Employee(employee.UserId));

        var received = await employee.NextAsync();
        received.GetProperty("id").GetGuid().ShouldBe(notification.Id);
        received.GetProperty("type").GetString().ShouldBe("TransactionsToReview");
        received.GetProperty("title").GetString().ShouldBe(notification.Title);
        received.GetProperty("body").GetString().ShouldBe(notification.Body);
        received.GetProperty("isRead").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task A_notification_reaches_only_its_recipient_even_when_a_company_shares_the_id()
    {
        using var factory = new HubWebApplicationFactory();
        var sharedId = Guid.CreateVersion7();
        await using var employee = await Listener.ConnectAsync(factory, sharedId, Rol.Empleado);
        await using var company = await Listener.ConnectAsync(factory, sharedId, Rol.Empresa);
        await using var otherEmployee = await Listener.ConnectAsync(factory, Guid.CreateVersion7(), Rol.Empleado);

        var forEmployee = await SendAsync(factory, NotificationRecipient.Employee(sharedId));
        var forCompany = await SendAsync(factory, NotificationRecipient.Company(sharedId));
        var forOtherEmployee = await SendAsync(factory, NotificationRecipient.Employee(otherEmployee.UserId));

        (await employee.NextAsync()).GetProperty("id").GetGuid().ShouldBe(forEmployee.Id);
        (await company.NextAsync()).GetProperty("id").GetGuid().ShouldBe(forCompany.Id);
        (await otherEmployee.NextAsync()).GetProperty("id").GetGuid().ShouldBe(forOtherEmployee.Id);
    }

    [Fact]
    public async Task Every_open_tab_of_the_same_employee_receives_the_notification()
    {
        using var factory = new HubWebApplicationFactory();
        var employeeId = Guid.CreateVersion7();
        await using var firstTab = await Listener.ConnectAsync(factory, employeeId, Rol.Empleado);
        await using var secondTab = await Listener.ConnectAsync(factory, employeeId, Rol.Empleado);

        var notification = await SendAsync(factory, NotificationRecipient.Employee(employeeId));

        (await firstTab.NextAsync()).GetProperty("id").GetGuid().ShouldBe(notification.Id);
        (await secondTab.NextAsync()).GetProperty("id").GetGuid().ShouldBe(notification.Id);
    }

    private static async Task<Notification> SendAsync(HubWebApplicationFactory factory, NotificationRecipient recipient)
    {
        var notification = Notification.Create(
            recipient,
            NotificationType.TransactionsToReview,
            "Movimientos para revisar",
            "Entraron movimientos nuevos desde Mercado Pago.",
            "transactions-to-review:MercadoPago",
            DateTimeOffset.UtcNow);

        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetServices<INotificationSender>()
            .Single(candidate => candidate.Channel == NotificationChannel.InApp);

        (await sender.SendAsync(notification)).ShouldBeTrue();

        return notification;
    }

    private static HubConnection BuildConnection(HubWebApplicationFactory factory, string? sessionToken) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, HubPath), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();

                if (sessionToken is not null)
                {
                    options.Headers["Cookie"] = $"{SessionCookie.Name}={sessionToken}";
                }
            })
            .Build();

    private sealed class Listener : IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private readonly Channel<JsonElement> _received = Channel.CreateUnbounded<JsonElement>();

        private Listener(HubConnection connection, Guid userId)
        {
            _connection = connection;
            UserId = userId;
            _connection.On<JsonElement>("NotificationReceived", notification => _received.Writer.TryWrite(notification));
        }

        public Guid UserId { get; }

        public static async Task<Listener> ConnectAsync(HubWebApplicationFactory factory, Guid userId, string role)
        {
            using var scope = factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
                .GenerateToken(userId, userId, role, "Juan Perez");

            var listener = new Listener(BuildConnection(factory, token.Value), userId);
            await listener._connection.StartAsync();

            return listener;
        }

        public async Task<JsonElement> NextAsync()
        {
            using var timeout = new CancellationTokenSource(ReceiveTimeout);

            return await _received.Reader.ReadAsync(timeout.Token);
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    private sealed class HubWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
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
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key",
                }));
    }
}

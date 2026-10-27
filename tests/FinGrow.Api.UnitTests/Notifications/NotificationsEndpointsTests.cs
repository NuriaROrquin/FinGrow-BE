namespace FinGrow.Api.UnitTests.Notifications;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

public class NotificationsEndpointsTests
{
    private const string NotificationsPath = "/api/notifications";
    private static readonly DateTimeOffset AnHourAgo = DateTimeOffset.UtcNow.AddHours(-1);

    [Fact]
    public async Task Without_a_session_the_inbox_responds_401()
    {
        using var factory = new NotificationsWebApplicationFactory();

        var response = await factory.CreateClient().GetAsync(new Uri(NotificationsPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_employee_sees_only_their_notifications_and_their_unread_count()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var own = factory.Store(NotificationRecipient.Employee(factory.UserId));
        factory.Store(NotificationRecipient.Company(factory.UserId));
        factory.Store(NotificationRecipient.Employee(Guid.CreateVersion7()));
        var client = CreateClient(factory, Rol.Empleado);

        var inbox = await client.GetFromJsonAsync<JsonElement>(new Uri(NotificationsPath, UriKind.Relative));
        var unread = await client.GetFromJsonAsync<JsonElement>(new Uri(NotificationsPath + "/unread-count", UriKind.Relative));

        inbox.GetProperty("totalCount").GetInt32().ShouldBe(1);
        var item = inbox.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("id").GetGuid().ShouldBe(own.Id);
        item.GetProperty("type").GetString().ShouldBe("TransactionsToReview");
        item.GetProperty("isRead").GetBoolean().ShouldBeFalse();
        unread.GetProperty("unread").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task A_company_sees_the_notifications_addressed_to_the_company()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var forCompany = factory.Store(NotificationRecipient.Company(factory.UserId));
        factory.Store(NotificationRecipient.Employee(factory.UserId));
        var client = CreateClient(factory, Rol.Empresa);

        var inbox = await client.GetFromJsonAsync<JsonElement>(new Uri(NotificationsPath, UriKind.Relative));

        inbox.GetProperty("items").EnumerateArray().ShouldHaveSingleItem().GetProperty("id").GetGuid().ShouldBe(forCompany.Id);
    }

    [Fact]
    public async Task A_page_size_out_of_range_responds_400()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var client = CreateClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri(NotificationsPath + "?pageSize=500", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reading_a_notification_marks_it_and_someone_else_one_responds_403()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var own = factory.Store(NotificationRecipient.Employee(factory.UserId));
        var foreign = factory.Store(NotificationRecipient.Employee(Guid.CreateVersion7()));
        var client = CreateClient(factory, Rol.Empleado);

        var read = await client.PostAsync(new Uri($"{NotificationsPath}/{own.Id}/read", UriKind.Relative), content: null);
        var forbidden = await client.PostAsync(new Uri($"{NotificationsPath}/{foreign.Id}/read", UriKind.Relative), content: null);

        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await read.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isRead").GetBoolean().ShouldBeTrue();
        own.IsRead.ShouldBeTrue();
        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        foreign.IsRead.ShouldBeFalse();
    }

    [Fact]
    public async Task Marking_all_as_read_reports_how_many_were_marked()
    {
        using var factory = new NotificationsWebApplicationFactory();
        factory.Store(NotificationRecipient.Employee(factory.UserId));
        factory.Store(NotificationRecipient.Employee(factory.UserId));
        var client = CreateClient(factory, Rol.Empleado);

        var response = await client.PostAsync(new Uri(NotificationsPath + "/read-all", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("marked").GetInt32().ShouldBe(2);
        factory.Notifications.ShouldAllBe(notification => notification.IsRead);
    }

    [Fact]
    public async Task An_employee_sees_the_channels_and_can_turn_telegram_off()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var client = CreateClient(factory, Rol.Empleado);

        var channels = await client.GetFromJsonAsync<JsonElement>(new Uri(NotificationsPath + "/channels", UriKind.Relative));
        var change = await client.PutAsJsonAsync(
            new Uri(NotificationsPath + "/channels/Telegram", UriKind.Relative),
            new { isEnabled = false });

        var names = channels.EnumerateArray().Select(channel => channel.GetProperty("channel").GetString()).ToList();
        names.Count.ShouldBe(3);
        names.ShouldContain("InApp");
        names.ShouldContain("Telegram");
        names.ShouldContain("Email");
        change.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await change.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isEnabled").GetBoolean().ShouldBeFalse();
        var stored = factory.ChannelSettings.ShouldHaveSingleItem();
        stored.Channel.ShouldBe(NotificationChannel.Telegram);
        stored.IsEnabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData("InApp")]
    [InlineData("Paloma")]
    public async Task A_channel_that_cannot_be_configured_responds_400(string channel)
    {
        using var factory = new NotificationsWebApplicationFactory();
        var client = CreateClient(factory, Rol.Empleado);

        var response = await client.PutAsJsonAsync(
            new Uri($"{NotificationsPath}/channels/{channel}", UriKind.Relative),
            new { isEnabled = false });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.ChannelSettings.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_company_has_no_channels_to_configure()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var client = CreateClient(factory, Rol.Empresa);

        var response = await client.GetAsync(new Uri(NotificationsPath + "/channels", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_employee_sees_every_alert_type_and_can_turn_one_off()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var client = CreateClient(factory, Rol.Empleado);

        var types = await client.GetFromJsonAsync<JsonElement>(new Uri(NotificationsPath + "/types", UriKind.Relative));
        var change = await client.PutAsJsonAsync(
            new Uri(NotificationsPath + "/types/BudgetWarning", UriKind.Relative),
            new { isEnabled = false });

        var items = types.EnumerateArray().ToList();
        var companyOnlyTypes = 5;
        items.Count.ShouldBe(Enum.GetValues<NotificationType>().Length - companyOnlyTypes);
        items.ShouldAllBe(item => item.GetProperty("isEnabled").GetBoolean());
        change.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await change.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("type").GetString().ShouldBe("BudgetWarning");
        body.GetProperty("isEnabled").GetBoolean().ShouldBeFalse();
        var stored = factory.TypeSettings.ShouldHaveSingleItem();
        stored.Type.ShouldBe(NotificationType.BudgetWarning);
        stored.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task An_alert_type_that_does_not_exist_responds_400()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var client = CreateClient(factory, Rol.Empleado);

        var response = await client.PutAsJsonAsync(
            new Uri(NotificationsPath + "/types/Paloma", UriKind.Relative),
            new { isEnabled = false });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.TypeSettings.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_company_can_list_its_alert_types()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var client = CreateClient(factory, Rol.Empresa);

        var response = await client.GetAsync(new Uri(NotificationsPath + "/types", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_company_sees_the_monthly_report_turned_off_and_can_turn_it_on()
    {
        using var factory = new NotificationsWebApplicationFactory();
        var client = CreateClient(factory, Rol.Empresa);

        var types = await client.GetFromJsonAsync<JsonElement>(new Uri(NotificationsPath + "/types", UriKind.Relative));
        var change = await client.PutAsJsonAsync(
            new Uri(NotificationsPath + "/types/MonthlyReport", UriKind.Relative),
            new { isEnabled = true, cadence = "Monthly" });

        var monthlyReport = types.EnumerateArray()
            .Single(item => item.GetProperty("type").GetString() == "MonthlyReport");
        monthlyReport.GetProperty("isEnabled").GetBoolean().ShouldBeFalse();
        monthlyReport.GetProperty("threshold").ValueKind.ShouldBe(JsonValueKind.Null);
        change.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = factory.CompanyTypeSettings.ShouldHaveSingleItem();
        stored.Type.ShouldBe(NotificationType.MonthlyReport);
        stored.IsEnabled.ShouldBeTrue();
        stored.EnabledAt.ShouldNotBeNull();
    }

    private static HttpClient CreateClient(NotificationsWebApplicationFactory factory, string role)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(factory.UserId, factory.UserId, role, "Juan Perez", Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class NotificationsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Guid UserId { get; } = Guid.CreateVersion7();

        public List<Notification> Notifications { get; } = new();

        public List<NotificationChannelSetting> ChannelSettings { get; } = new();

        public List<NotificationTypeSetting> TypeSettings { get; } = new();

        public List<CompanyNotificationSetting> CompanyTypeSettings { get; } = new();

        public Notification Store(NotificationRecipient recipient)
        {
            var notification = Notification.Create(
                recipient,
                NotificationType.TransactionsToReview,
                "Movimientos para revisar",
                "Entraron movimientos nuevos desde Mercado Pago.",
                "transactions-to-review:MercadoPago",
                AnHourAgo);
            Notifications.Add(notification);

            return notification;
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
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["Gmail:ClientId"] = "unit-test-gmail-client-id",
                    ["Gmail:ClientSecret"] = "unit-test-gmail-client-secret",
                    ["Gmail:RedirectUri"] = "https://api.test/api/integrations/gmail/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.TreatEverySessionAsOpen();
                services.AddScoped<INotificationRepository>(_ => new InMemoryNotificationRepository(Notifications));
                services.AddScoped<INotificationChannelSettingRepository>(_ => new InMemoryChannelSettingRepository(ChannelSettings));
                services.AddScoped<INotificationTypeSettingRepository>(_ => new InMemoryTypeSettingRepository(TypeSettings));
                services.AddScoped<ICompanyNotificationSettingRepository>(_ => new InMemoryCompanyTypeSettingRepository(CompanyTypeSettings));
                services.AddScoped<IEmployeeIntegrationRepository, NoIntegrationsRepository>();
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    private sealed class InMemoryNotificationRepository(List<Notification> notifications) : INotificationRepository
    {
        public void Add(Notification notification) => notifications.Add(notification);

        public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(notifications.FirstOrDefault(notification => notification.Id == id));

        public Task<bool> ExistsSinceAsync(
            NotificationRecipient recipient,
            NotificationType type,
            string deduplicationKey,
            DateTimeOffset since,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Filtered(recipient, unreadOnly: false).Any(notification =>
                notification.Type == type && notification.DeduplicationKey == deduplicationKey && notification.CreatedAt >= since));

        public Task<IReadOnlyList<Notification>> ListAsync(
            NotificationRecipient recipient,
            bool unreadOnly,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(Filtered(recipient, unreadOnly).Skip(skip).Take(take).ToList());

        public Task<int> CountAsync(NotificationRecipient recipient, bool unreadOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult(Filtered(recipient, unreadOnly).Count());

        public Task<IReadOnlyList<Notification>> ListUnreadAsync(NotificationRecipient recipient, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(Filtered(recipient, unreadOnly: true).ToList());

        public Task RemoveAllForAsync(NotificationRecipient recipient, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        private IEnumerable<Notification> Filtered(NotificationRecipient recipient, bool unreadOnly) =>
            notifications.Where(notification => notification.IsAddressedTo(recipient) && (!unreadOnly || !notification.IsRead));
    }

    private sealed class InMemoryChannelSettingRepository(List<NotificationChannelSetting> settings) : INotificationChannelSettingRepository
    {
        public void Add(NotificationChannelSetting setting) => settings.Add(setting);

        public Task<IReadOnlyList<NotificationChannelSetting>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationChannelSetting>>(settings.Where(setting => setting.EmployeeId == employeeId).ToList());
    }

    private sealed class InMemoryTypeSettingRepository(List<NotificationTypeSetting> settings) : INotificationTypeSettingRepository
    {
        public void Add(NotificationTypeSetting setting) => settings.Add(setting);

        public Task<IReadOnlyList<NotificationTypeSetting>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationTypeSetting>>(settings.Where(setting => setting.EmployeeId == employeeId).ToList());
    }

    private sealed class InMemoryCompanyTypeSettingRepository(List<CompanyNotificationSetting> settings)
        : ICompanyNotificationSettingRepository
    {
        public void Add(CompanyNotificationSetting setting) => settings.Add(setting);

        public Task<IReadOnlyList<CompanyNotificationSetting>> ListByCompanyAsync(
            Guid companyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CompanyNotificationSetting>>(
                settings.Where(setting => setting.CompanyId == companyId).ToList());
    }

    private sealed class NoIntegrationsRepository : IEmployeeIntegrationRepository
    {
        public Task<EmployeeIntegration?> FindByExternalAccountAsync(
            IntegrationProvider provider,
            string externalAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EmployeeIntegration?>(null);

        public Task<EmployeeIntegration?> FindByEmployeeAsync(
            Guid employeeId,
            IntegrationProvider provider,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EmployeeIntegration?>(null);

        public Task<IReadOnlyList<EmployeeIntegration>> ListAuthorizedAsync(
            IntegrationProvider provider,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmployeeIntegration>>(Array.Empty<EmployeeIntegration>());

        public void Add(EmployeeIntegration integration)
        {
        }

        public void Remove(EmployeeIntegration integration)
        {
        }
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}

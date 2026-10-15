namespace FinGrow.Application.UnitTests.Features.Integrations.Gmail;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Gmail.Sync;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class GmailSyncJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeGmailMailboxClient _mailbox = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly ServiceProvider _provider;

    public GmailSyncJobTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddApplication();
        services.AddSingleton<IEmployeeIntegrationRepository>(_integrations);
        services.AddSingleton<IEmployeeRepository>(_employees);
        services.AddSingleton<IGmailMailboxClient>(_mailbox);
        services.AddSingleton<IGmailOAuthClient>(new FakeGmailOAuthClient());
        services.AddSingleton<IAiService>(new FakeAiService());
        services.AddSingleton<ITransactionRepository>(new FakeTransactionRepository());
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IDateTimeProvider>(new FakeDateTimeProvider(Now));
        services.AddSingleton<ICurrentUser>(_currentUser);

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Every_active_gmail_is_synchronized_and_failed_ones_wait_for_reauthorization()
    {
        var ana = Linked("ana@gmail.com", "token-ana");
        var bruno = Linked("bruno@gmail.com", "token-bruno");
        var failed = Linked("carla@gmail.com", "token-carla");
        failed.MarkFailed("Sin acceso", Now.AddDays(-1));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 Gmail vinculado(s): 2 sincronizado(s), 0 rechazado(s), 0 con error.");
        _mailbox.Searches.Select(search => search.AccessToken).Order(StringComparer.Ordinal).ToList()
            .ShouldBe(new List<string> { "token-ana", "token-bruno" });
        ana.LastSyncedAt.ShouldBe(Now);
        bruno.LastSyncedAt.ShouldBe(Now);
        failed.LastSyncedAt.ShouldBeNull();
    }

    [Fact]
    public async Task An_employee_can_sync_their_own_gmail_on_demand()
    {
        var ana = Linked("ana@gmail.com", "token-ana");
        Linked("bruno@gmail.com", "token-bruno");
        _currentUser.UserId = ana.EmployeeId;

        var result = await _provider.GetRequiredService<ISender>().Send(new SyncMyGmailCommand());

        result.IsSuccess.ShouldBeTrue();
        _mailbox.Searches.ShouldHaveSingleItem().AccessToken.ShouldBe("token-ana");
        ana.LastSyncedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Syncing_on_demand_without_a_session_is_forbidden()
    {
        var result = await _provider.GetRequiredService<ISender>().Send(new SyncMyGmailCommand());

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _mailbox.Searches.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_job_is_registered_with_a_daily_schedule()
    {
        var job = Job();

        job.Name.ShouldBe("gmail-sync");
        job.Schedule.ShouldBe("0 13 * * *");
        job.Description.ShouldNotBeNullOrWhiteSpace();
    }

    private GmailSyncJob Job() => new(
        _integrations,
        _provider.GetRequiredService<IServiceScopeFactory>(),
        NullLogger<GmailSyncJob>.Instance);

    private EmployeeIntegration Linked(string mailbox, string accessToken)
    {
        var employee = Employee.Create(
            Guid.CreateVersion7(),
            departmentId: null,
            "Empleado",
            Email.From($"{accessToken}@empresa.com"),
            null,
            "hash-bcrypt",
            Currency.ARS,
            new DateOnly(2026, 1, 15),
            Now);
        _employees.Employees.Add(employee);

        var integration = EmployeeIntegration.Create(employee.Id, IntegrationProvider.Gmail, mailbox, Now.AddDays(-10));
        integration.Authorize(OAuthGrant.From(accessToken, "refresh", Now.AddHours(1)), Now.AddDays(-10));
        _integrations.Add(integration);

        return integration;
    }
}

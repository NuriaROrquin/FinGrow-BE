namespace FinGrow.Application.UnitTests.Features.Integrations.Iol;

using System.Net;
using FinGrow.Application.Features.Integrations.Iol;
using FinGrow.Application.Features.Integrations.Iol.KeepAlive;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class IolTokenRefreshJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 3, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeIolAuthClient _iol = new() { Tokens = new("new-access", "new-refresh", TimeSpan.FromMinutes(15)) };
    private readonly ServiceProvider _provider;

    public IolTokenRefreshJobTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddApplication();
        services.AddSingleton<IEmployeeIntegrationRepository>(_integrations);
        services.AddSingleton<IIolAuthClient>(_iol);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IDateTimeProvider>(new FakeDateTimeProvider(Now));

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Only_the_authorizations_about_to_expire_are_renewed()
    {
        var due = Linked("ana", expiresAt: Now.AddMinutes(7));
        var current = Linked("bruno", expiresAt: Now.AddMinutes(14));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 cuenta(s) de IOL: 1 renovada(s), 1 vigente(s), 0 vencida(s), 0 con error.");
        due.Grant!.AccessToken.ShouldBe("new-access");
        due.Grant.ExpiresAt.ShouldBe(Now.AddMinutes(15));
        current.Grant!.AccessToken.ShouldBe("access-bruno");
    }

    [Fact]
    public async Task Running_every_five_minutes_keeps_an_authorization_alive_overnight()
    {
        var integration = Linked("carla", expiresAt: Now.AddMinutes(15));
        var clock = (FakeDateTimeProvider)_provider.GetRequiredService<IDateTimeProvider>();

        for (var minute = 5; minute <= 8 * 60; minute += 5)
        {
            clock.UtcNow = Now.AddMinutes(minute);
            await Job().ExecuteAsync(CancellationToken.None);

            integration.Grant!.ExpiresAt.ShouldBeGreaterThan(clock.UtcNow);
        }

        integration.Status.ShouldBe(IntegrationStatus.Active);
    }

    [Fact]
    public async Task A_revoked_authorization_is_counted_as_expired_and_the_run_still_succeeds()
    {
        var revoked = Linked("diego", expiresAt: Now.AddMinutes(5));
        _iol.Failure = new HttpRequestException("invalid_grant", null, HttpStatusCode.BadRequest);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("1 cuenta(s) de IOL: 0 renovada(s), 0 vigente(s), 1 vencida(s), 0 con error.");
        revoked.Status.ShouldBe(IntegrationStatus.Error);
    }

    [Fact]
    public async Task When_iol_is_down_with_the_bearer_already_expired_the_run_fails_but_the_link_survives()
    {
        var integration = Linked("elena", expiresAt: Now.AddMinutes(-1));
        _iol.Failure = new TaskCanceledException("timeout");

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.ShouldContain(IolErrors.Unavailable.Description);
        integration.Status.ShouldBe(IntegrationStatus.Active);
    }

    [Fact]
    public async Task Accounts_in_error_are_not_visited()
    {
        var failed = Linked("fabio", expiresAt: Now.AddMinutes(5));
        failed.MarkFailed(IolErrors.GrantExpired.Description, Now.AddHours(-1));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.Summary.ShouldBe("0 cuenta(s) de IOL: 0 renovada(s), 0 vigente(s), 0 vencida(s), 0 con error.");
        _iol.Refreshes.ShouldBeEmpty();
    }

    [Fact]
    public void The_job_runs_every_five_minutes_all_day()
    {
        var job = Job();

        job.Name.ShouldBe("iol-token-refresh");
        job.Schedule.ShouldBe("*/5 * * * *");

        var services = new ServiceCollection();
        services.AddApplication();
        services.ShouldContain(service =>
            service.ServiceType == typeof(IScheduledJob) && service.ImplementationType == typeof(IolTokenRefreshJob));
    }

    private IolTokenRefreshJob Job() => new(
        _integrations,
        _provider.GetRequiredService<IServiceScopeFactory>(),
        NullLogger<IolTokenRefreshJob>.Instance);

    private EmployeeIntegration Linked(string account, DateTimeOffset expiresAt)
    {
        var integration = EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.Iol, account, Now.AddDays(-1));
        integration.Authorize(OAuthGrant.From($"access-{account}", "refresh", expiresAt), Now.AddDays(-1));
        _integrations.Add(integration);

        return integration;
    }
}

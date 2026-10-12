namespace FinGrow.Application.UnitTests.Features.Integrations.Iol;

using FinGrow.Application.Features.Integrations.Iol;
using FinGrow.Application.Features.Integrations.Iol.Sync;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class IolSyncJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeIolPortfolioClient _iol = new();
    private readonly FakeInvestmentRepository _investments = new();
    private readonly ServiceProvider _provider;

    public IolSyncJobTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddApplication();
        services.AddSingleton<IEmployeeIntegrationRepository>(_integrations);
        services.AddSingleton<IIolAuthClient>(new FakeIolAuthClient());
        services.AddSingleton<IIolPortfolioClient>(_iol);
        services.AddSingleton<IInvestmentRepository>(_investments);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IDateTimeProvider>(new FakeDateTimeProvider(Now));

        _provider = services.BuildServiceProvider();
        _iol.Portfolios[IolCountry.Argentina].Add(new IolPosition(
            "GGAL", "Grupo Financiero Galicia", "ACCIONES", "bCBA", "peso_Argentino", 10m, 5000m, 56_000m));
    }

    [Fact]
    public async Task Every_linked_account_is_synchronized_and_the_summary_counts_them()
    {
        var ana = Linked("ana", "token-ana");
        var bruno = Linked("bruno", "token-bruno");

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 cuenta(s) vinculada(s): 2 sincronizada(s), 0 rechazada(s), 0 con error.");
        _investments.Investments.Select(investment => investment.EmployeeId).ShouldBe(new[] { ana.EmployeeId, bruno.EmployeeId }, ignoreOrder: true);
    }

    [Fact]
    public async Task When_iol_does_not_answer_for_an_account_the_run_fails_and_the_others_still_sync()
    {
        var unanswered = Linked("carla", "token-carla");
        var healthy = Linked("diego", "token-diego");
        _iol.UnavailableAccessToken = "token-carla";

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Summary.ShouldBe("2 cuenta(s) vinculada(s): 1 sincronizada(s), 0 rechazada(s), 1 con error.");
        result.Error!.ShouldContain(unanswered.EmployeeId.ToString());
        result.Error!.ShouldContain(IolErrors.Unavailable.Description);
        _investments.Investments.ShouldHaveSingleItem().EmployeeId.ShouldBe(healthy.EmployeeId);
        unanswered.LastSyncedAt.ShouldBeNull();
    }

    [Fact]
    public async Task An_authorization_that_can_no_longer_be_renewed_counts_as_rejected()
    {
        var expired = Linked("elena", "token-elena", expiresAt: Now.AddMinutes(-5), refreshToken: null);
        Linked("fabio", "token-fabio");

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 cuenta(s) vinculada(s): 1 sincronizada(s), 1 rechazada(s), 0 con error.");
        expired.Status.ShouldBe(IntegrationStatus.Error);
    }

    [Fact]
    public async Task Accounts_in_error_are_not_visited()
    {
        var failed = Linked("gina", "token-gina");
        failed.MarkFailed(IolErrors.GrantExpired.Description, Now.AddDays(-1));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.Summary.ShouldBe("0 cuenta(s) vinculada(s): 0 sincronizada(s), 0 rechazada(s), 0 con error.");
        _iol.PortfolioRequests.ShouldBeEmpty();
    }

    [Fact]
    public void The_job_runs_every_half_hour_while_the_market_is_open()
    {
        var job = Job();

        job.Name.ShouldBe("iol-sync");
        job.Schedule.ShouldBe("*/30 14-21 * * 1-5");
        job.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void The_job_is_registered_in_the_application()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.ShouldContain(service =>
            service.ServiceType == typeof(FinGrow.Application.Jobs.IScheduledJob)
            && service.ImplementationType == typeof(IolSyncJob));
    }

    private IolSyncJob Job() => new(
        _integrations,
        _provider.GetRequiredService<IServiceScopeFactory>(),
        NullLogger<IolSyncJob>.Instance);

    private EmployeeIntegration Linked(string account, string accessToken, DateTimeOffset? expiresAt = null, string? refreshToken = "refresh")
    {
        var integration = EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.Iol, account, Now.AddDays(-10));
        integration.Authorize(OAuthGrant.From(accessToken, refreshToken, expiresAt ?? Now.AddMinutes(10)), Now.AddDays(-10));
        _integrations.Add(integration);

        return integration;
    }
}

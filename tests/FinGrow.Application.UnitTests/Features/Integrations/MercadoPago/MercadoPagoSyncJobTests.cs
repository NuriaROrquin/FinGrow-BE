namespace FinGrow.Application.UnitTests.Features.Integrations.MercadoPago;

using FinGrow.Application.Features.Integrations.MercadoPago.Sync;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class MercadoPagoSyncJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeMercadoPagoPaymentsClient _payments = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly ServiceProvider _provider;

    public MercadoPagoSyncJobTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddApplication();
        services.AddSingleton<IEmployeeIntegrationRepository>(_integrations);
        services.AddSingleton<IMercadoPagoPaymentsClient>(_payments);
        services.AddSingleton<IMercadoPagoOAuthClient>(new FakeMercadoPagoOAuthClient());
        services.AddSingleton<IAiService>(new FakeAiService());
        services.AddSingleton<ITransactionRepository>(_transactions);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IDateTimeProvider>(new FakeDateTimeProvider(Now));

        _provider = services.BuildServiceProvider();
        _payments.Payments.Add(new MercadoPagoPayment(
            1, "approved", "regular_payment", 10285m, "ARS", "Canva", "debit_card",
            CollectorId: null, PayerId: null, Now.AddDays(-2), Now.AddDays(-2)));
    }

    [Fact]
    public async Task Every_linked_account_is_synchronized_and_the_summary_counts_them()
    {
        var ana = Linked("ana", "token-ana");
        var bruno = Linked("bruno", "token-bruno");

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 cuenta(s) vinculada(s): 2 sincronizada(s), 0 rechazada(s), 0 con error.");
        _transactions.Transactions.Select(transaction => transaction.EmployeeId).ShouldBe(new[] { ana.EmployeeId, bruno.EmployeeId }, ignoreOrder: true);
        ana.LastSyncedAt.ShouldBe(Now);
        bruno.LastSyncedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task An_expired_authorization_is_reported_as_rejected_without_stopping_the_others()
    {
        var expired = Linked("carla", "token-carla", expiresAt: Now.AddDays(-1), refreshToken: null);
        var healthy = Linked("diego", "token-diego");

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 cuenta(s) vinculada(s): 1 sincronizada(s), 1 rechazada(s), 0 con error.");
        _transactions.Transactions.ShouldHaveSingleItem().EmployeeId.ShouldBe(healthy.EmployeeId);
        expired.LastSyncedAt.ShouldBeNull();
    }

    [Fact]
    public async Task A_revoked_authorization_counts_as_rejected_and_does_not_fail_the_run()
    {
        var revoked = Linked("gina", "token-gina", refreshToken: null);
        var healthy = Linked("hugo", "token-hugo");
        _payments.UnauthorizedAccessToken = "token-gina";

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 cuenta(s) vinculada(s): 1 sincronizada(s), 1 rechazada(s), 0 con error.");
        _transactions.Transactions.ShouldHaveSingleItem().EmployeeId.ShouldBe(healthy.EmployeeId);
        revoked.LastSyncedAt.ShouldBeNull();
    }

    [Fact]
    public async Task A_technical_failure_in_one_account_marks_the_run_as_failed_but_the_rest_still_sync()
    {
        var broken = Linked("elena", "token-elena");
        var healthy = Linked("fabio", "token-fabio");
        _payments.BrokenAccessToken = "token-elena";

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Summary.ShouldBe("2 cuenta(s) vinculada(s): 1 sincronizada(s), 0 rechazada(s), 1 con error.");
        result.Error!.ShouldContain(broken.EmployeeId.ToString());
        result.Error!.ShouldContain("InvalidOperationException");
        _transactions.Transactions.ShouldHaveSingleItem().EmployeeId.ShouldBe(healthy.EmployeeId);
    }

    [Fact]
    public async Task Accounts_that_never_authorized_are_not_visited()
    {
        _integrations.Add(EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.MercadoPago, "999", Now));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("0 cuenta(s) vinculada(s): 0 sincronizada(s), 0 rechazada(s), 0 con error.");
        _payments.Searches.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_job_is_registered_with_an_hourly_schedule()
    {
        var job = Job();

        job.Name.ShouldBe("mercadopago-sync");
        job.Schedule.ShouldBe("0 * * * *");
        job.Description.ShouldNotBeNullOrWhiteSpace();
    }

    private MercadoPagoSyncJob Job() => new(
        _integrations,
        _provider.GetRequiredService<IServiceScopeFactory>(),
        NullLogger<MercadoPagoSyncJob>.Instance);

    private EmployeeIntegration Linked(string account, string accessToken, DateTimeOffset? expiresAt = null, string? refreshToken = "refresh")
    {
        var integration = EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.MercadoPago, account, Now.AddDays(-10));
        integration.Authorize(OAuthGrant.From(accessToken, refreshToken, expiresAt ?? Now.AddDays(170)), Now.AddDays(-10));
        _integrations.Add(integration);

        return integration;
    }
}

namespace FinGrow.Application.UnitTests.Jobs;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class JobRunnerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeJobRunRepository _runs = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly FakeJob _job = new("fake-job");
    private readonly ServiceProvider _provider;

    public JobRunnerTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<RunningJobs>();
        services.AddSingleton<IJobRunRepository>(_runs);
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddSingleton<IDateTimeProvider>(_clock);
        services.AddScoped<IScheduledJob>(_ => _job);
        services.AddScoped<JobRunner>();

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Running_a_job_records_a_succeeded_run_with_its_summary()
    {
        _job.Behavior = _ => Task.FromResult(JobResult.Success("3 fotos generadas"));

        var result = await Run("fake-job");

        result.IsSuccess.ShouldBeTrue();
        var run = _runs.Runs.ShouldHaveSingleItem();
        run.ShouldBe(result.Value);
        run.JobName.ShouldBe("fake-job");
        run.Status.ShouldBe(JobRunStatus.Succeeded);
        run.Summary.ShouldBe("3 fotos generadas");
        run.StartedAt.ShouldBe(Now);
        run.FinishedAt.ShouldBe(Now);
        _job.Executions.ShouldBe(1);
        _unitOfWork.SaveCount.ShouldBe(2);
    }

    [Fact]
    public async Task A_job_that_reports_a_failure_gets_its_error_recorded()
    {
        _job.Behavior = _ => Task.FromResult(JobResult.Failure("Mercado Pago no respondio", "2 cuentas: 1 con error"));

        var result = await Run("fake-job");

        result.IsSuccess.ShouldBeTrue();
        var run = _runs.Runs.ShouldHaveSingleItem();
        run.Status.ShouldBe(JobRunStatus.Failed);
        run.Error.ShouldBe("Mercado Pago no respondio");
        run.Summary.ShouldBe("2 cuentas: 1 con error");
    }

    [Fact]
    public async Task A_job_that_throws_gets_the_exception_recorded_and_the_next_run_still_executes()
    {
        _job.Behavior = _ => throw new InvalidOperationException("boom");

        var failed = await Run("fake-job");

        failed.IsSuccess.ShouldBeTrue();
        failed.Value.Status.ShouldBe(JobRunStatus.Failed);
        failed.Value.Error.ShouldBe("InvalidOperationException: boom");
        failed.Value.FinishedAt.ShouldNotBeNull();

        _job.Behavior = _ => Task.FromResult(JobResult.Success("ahora si"));
        _clock.UtcNow = Now.AddHours(1);

        var recovered = await Run("fake-job");

        recovered.Value.Status.ShouldBe(JobRunStatus.Succeeded);
        _runs.Runs.Count.ShouldBe(2);
        _job.Executions.ShouldBe(2);
    }

    [Fact]
    public async Task A_cancelled_run_is_recorded_as_failed_instead_of_staying_running()
    {
        using var cancellation = new CancellationTokenSource();
        _job.Behavior = token =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult(JobResult.Success());
        };

        var result = await Run("fake-job", cancellation.Token);

        result.Value.Status.ShouldBe(JobRunStatus.Failed);
        result.Value.Error!.ShouldContain("se cancelo");
    }

    [Fact]
    public async Task An_unknown_job_is_rejected_and_nothing_is_recorded()
    {
        var result = await Run("no-existe");

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _runs.Runs.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_job_already_running_is_rejected_with_a_conflict()
    {
        var release = new TaskCompletionSource();
        var started = new TaskCompletionSource();
        _job.Behavior = async _ =>
        {
            started.SetResult();
            await release.Task;
            return JobResult.Success();
        };

        var first = Run("fake-job");
        await started.Task;

        var second = await Run("fake-job");

        second.IsFailure.ShouldBeTrue();
        second.Error.Type.ShouldBe(ErrorType.Conflict);

        release.SetResult();
        (await first).Value.Status.ShouldBe(JobRunStatus.Succeeded);
        _runs.Runs.ShouldHaveSingleItem();

        var third = await Run("fake-job");

        third.IsSuccess.ShouldBeTrue();
        _runs.Runs.Count.ShouldBe(2);
    }

    private async Task<Result<Domain.Entities.JobRun>> Run(string jobName, CancellationToken cancellationToken = default)
    {
        using var scope = _provider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<JobRunner>();

        return await runner.RunAsync(jobName, cancellationToken);
    }

    private sealed class FakeJob : IScheduledJob
    {
        public FakeJob(string name) => Name = name;

        public string Name { get; }

        public string Description => "Trabajo de prueba";

        public string Schedule => "* * * * *";

        public int Executions { get; private set; }

        public Func<CancellationToken, Task<JobResult>> Behavior { get; set; } = _ => Task.FromResult(JobResult.Success());

        public Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            Executions++;

            return Behavior(cancellationToken);
        }
    }
}

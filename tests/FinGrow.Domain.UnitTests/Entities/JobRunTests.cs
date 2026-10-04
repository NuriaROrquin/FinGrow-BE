namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public class JobRunTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_run_is_running_and_has_no_outcome_yet()
    {
        var run = JobRun.Start("metrics-snapshot", StartedAt);

        run.Status.ShouldBe(JobRunStatus.Running);
        run.IsFinished.ShouldBeFalse();
        run.JobName.ShouldBe("metrics-snapshot");
        run.StartedAt.ShouldBe(StartedAt);
        run.FinishedAt.ShouldBeNull();
        run.Duration.ShouldBeNull();
        run.Summary.ShouldBeNull();
        run.Error.ShouldBeNull();
    }

    [Fact]
    public void Succeeding_records_the_finish_time_and_the_summary()
    {
        var run = JobRun.Start("metrics-snapshot", StartedAt);

        run.Succeed(StartedAt.AddSeconds(90), "  3 fotos generadas  ");

        run.Status.ShouldBe(JobRunStatus.Succeeded);
        run.IsFinished.ShouldBeTrue();
        run.FinishedAt.ShouldBe(StartedAt.AddSeconds(90));
        run.Duration.ShouldBe(TimeSpan.FromSeconds(90));
        run.Summary.ShouldBe("3 fotos generadas");
        run.Error.ShouldBeNull();
    }

    [Fact]
    public void Failing_records_the_error_and_keeps_the_partial_summary()
    {
        var run = JobRun.Start("mercadopago-sync", StartedAt);

        run.Fail(StartedAt.AddSeconds(5), "Mercado Pago no respondio", "2 cuentas: 1 sincronizada, 1 con error");

        run.Status.ShouldBe(JobRunStatus.Failed);
        run.Error.ShouldBe("Mercado Pago no respondio");
        run.Summary.ShouldBe("2 cuentas: 1 sincronizada, 1 con error");
        run.FinishedAt.ShouldBe(StartedAt.AddSeconds(5));
    }

    [Fact]
    public void A_finished_run_cannot_finish_again()
    {
        var run = JobRun.Start("metrics-snapshot", StartedAt);
        run.Succeed(StartedAt.AddSeconds(1), null);

        Should.Throw<DomainException>(() => run.Fail(StartedAt.AddSeconds(2), "tarde"));
        Should.Throw<DomainException>(() => run.Succeed(StartedAt.AddSeconds(2), null));
    }

    [Fact]
    public void A_run_cannot_finish_before_it_started()
    {
        var run = JobRun.Start("metrics-snapshot", StartedAt);

        Should.Throw<DomainException>(() => run.Succeed(StartedAt.AddSeconds(-1), null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_failure_without_an_error_is_rejected(string error)
    {
        var run = JobRun.Start("metrics-snapshot", StartedAt);

        Should.Throw<DomainException>(() => run.Fail(StartedAt.AddSeconds(1), error));
        run.Status.ShouldBe(JobRunStatus.Running);
    }

    [Fact]
    public void Long_errors_and_summaries_are_truncated_instead_of_rejected()
    {
        var run = JobRun.Start("metrics-snapshot", StartedAt);

        run.Fail(StartedAt.AddSeconds(1), new string('e', JobRun.MaxErrorLength + 50), new string('s', JobRun.MaxSummaryLength + 50));

        run.Error!.Length.ShouldBe(JobRun.MaxErrorLength);
        run.Summary!.Length.ShouldBe(JobRun.MaxSummaryLength);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_run_needs_a_job_name(string jobName)
    {
        Should.Throw<DomainException>(() => JobRun.Start(jobName, StartedAt));
    }

    [Fact]
    public void A_job_name_longer_than_allowed_is_rejected()
    {
        Should.Throw<DomainException>(() => JobRun.Start(new string('j', JobRun.MaxJobNameLength + 1), StartedAt));
    }
}

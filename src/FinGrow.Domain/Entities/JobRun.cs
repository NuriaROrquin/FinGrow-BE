namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class JobRun : AggregateRoot
{
    public const int MaxJobNameLength = 100;
    public const int MaxSummaryLength = 1000;
    public const int MaxErrorLength = 4000;

    private JobRun()
    {
    }

    private JobRun(Guid id, string jobName, DateTimeOffset startedAt)
        : base(id)
    {
        JobName = jobName;
        StartedAt = startedAt;
        Status = JobRunStatus.Running;
    }

    public string JobName { get; private set; } = string.Empty;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public JobRunStatus Status { get; private set; }

    public string? Summary { get; private set; }

    public string? Error { get; private set; }

    public bool IsFinished => Status != JobRunStatus.Running;

    public TimeSpan? Duration => FinishedAt - StartedAt;

    public static JobRun Start(string jobName, DateTimeOffset startedAt)
    {
        var trimmed = (jobName ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("El nombre del trabajo es obligatorio."),
            > MaxJobNameLength => throw new DomainException(
                $"El nombre del trabajo no puede superar los {MaxJobNameLength} caracteres."),
            _ => new JobRun(Guid.CreateVersion7(), trimmed, startedAt)
        };
    }

    public void Succeed(DateTimeOffset finishedAt, string? summary)
    {
        EnsureRunning();
        Finish(finishedAt, summary);
        Status = JobRunStatus.Succeeded;
    }

    public void Fail(DateTimeOffset finishedAt, string error, string? summary = null)
    {
        EnsureRunning();

        var trimmedError = (error ?? string.Empty).Trim();

        if (trimmedError.Length == 0)
        {
            throw new DomainException("Una corrida fallida tiene que registrar el error.");
        }

        Finish(finishedAt, summary);
        Error = Truncate(trimmedError, MaxErrorLength);
        Status = JobRunStatus.Failed;
    }

    private void Finish(DateTimeOffset finishedAt, string? summary)
    {
        if (finishedAt < StartedAt)
        {
            throw new DomainException("Una corrida no puede terminar antes de haber empezado.");
        }

        FinishedAt = finishedAt;
        Summary = Truncate(summary?.Trim(), MaxSummaryLength);
    }

    private void EnsureRunning()
    {
        if (IsFinished)
        {
            throw new DomainException("La corrida ya habia terminado.");
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

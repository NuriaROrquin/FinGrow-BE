namespace FinGrow.Application.Features.Jobs;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public sealed record JobRunResponse(
    Guid Id,
    string JobName,
    JobRunStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    double? DurationSeconds,
    string? Summary,
    string? Error)
{
    public static JobRunResponse From(JobRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return new JobRunResponse(
            run.Id,
            run.JobName,
            run.Status,
            run.StartedAt,
            run.FinishedAt,
            run.Duration?.TotalSeconds,
            run.Summary,
            run.Error);
    }
}

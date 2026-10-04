namespace FinGrow.Application.Jobs;

public interface IScheduledJob
{
    string Name { get; }

    string Description { get; }

    string Schedule { get; }

    Task<JobResult> ExecuteAsync(CancellationToken cancellationToken);
}

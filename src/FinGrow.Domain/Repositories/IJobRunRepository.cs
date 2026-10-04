namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IJobRunRepository
{
    void Add(JobRun run);

    Task<JobRun?> FindLastAsync(string jobName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobRun>> ListRecentAsync(string jobName, int take, CancellationToken cancellationToken = default);
}

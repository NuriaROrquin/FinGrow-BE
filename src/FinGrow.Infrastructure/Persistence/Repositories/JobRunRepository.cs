namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class JobRunRepository : IJobRunRepository
{
    private readonly FinGrowDbContext _dbContext;

    public JobRunRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public void Add(JobRun run) => _dbContext.JobRuns.Add(run);

    public Task<JobRun?> FindLastAsync(string jobName, CancellationToken cancellationToken = default) =>
        MostRecentFirst(jobName).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<JobRun>> ListRecentAsync(string jobName, int take, CancellationToken cancellationToken = default) =>
        await MostRecentFirst(jobName).Take(take).ToListAsync(cancellationToken);

    private IQueryable<JobRun> MostRecentFirst(string jobName) =>
        _dbContext.JobRuns
            .AsNoTracking()
            .Where(run => run.JobName == jobName)
            .OrderByDescending(run => run.StartedAt)
            .ThenByDescending(run => run.Id);
}

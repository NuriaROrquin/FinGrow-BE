namespace FinGrow.Application.Features.Jobs.ListJobs;

using FinGrow.Application.Common;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListJobsHandler : IRequestHandler<ListJobsQuery, Result<IReadOnlyList<JobResponse>>>
{
    private readonly IEnumerable<IScheduledJob> _jobs;
    private readonly IJobRunRepository _runs;

    public ListJobsHandler(IEnumerable<IScheduledJob> jobs, IJobRunRepository runs)
    {
        _jobs = jobs;
        _runs = runs;
    }

    public async Task<Result<IReadOnlyList<JobResponse>>> Handle(ListJobsQuery request, CancellationToken cancellationToken)
    {
        var jobs = new List<JobResponse>();

        foreach (var job in _jobs.OrderBy(job => job.Name, StringComparer.Ordinal))
        {
            var lastRun = await _runs.FindLastAsync(job.Name, cancellationToken);

            jobs.Add(new JobResponse(
                job.Name,
                job.Description,
                job.Schedule,
                lastRun is null ? null : JobRunResponse.From(lastRun)));
        }

        return Result.Success<IReadOnlyList<JobResponse>>(jobs);
    }
}

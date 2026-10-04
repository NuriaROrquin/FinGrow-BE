namespace FinGrow.Application.Features.Jobs.ListJobRuns;

using FinGrow.Application.Common;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListJobRunsHandler : IRequestHandler<ListJobRunsQuery, Result<IReadOnlyList<JobRunResponse>>>
{
    private readonly IEnumerable<IScheduledJob> _jobs;
    private readonly IJobRunRepository _runs;

    public ListJobRunsHandler(IEnumerable<IScheduledJob> jobs, IJobRunRepository runs)
    {
        _jobs = jobs;
        _runs = runs;
    }

    public async Task<Result<IReadOnlyList<JobRunResponse>>> Handle(ListJobRunsQuery request, CancellationToken cancellationToken)
    {
        if (!_jobs.Any(job => string.Equals(job.Name, request.JobName, StringComparison.Ordinal)))
        {
            return Result.Failure<IReadOnlyList<JobRunResponse>>(JobErrors.NotFound(request.JobName));
        }

        var runs = await _runs.ListRecentAsync(request.JobName, request.Take, cancellationToken);

        return Result.Success<IReadOnlyList<JobRunResponse>>(runs.Select(JobRunResponse.From).ToList());
    }
}

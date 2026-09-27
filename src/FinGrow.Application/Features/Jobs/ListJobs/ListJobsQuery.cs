namespace FinGrow.Application.Features.Jobs.ListJobs;

using FinGrow.Application.Common;
using MediatR;

public sealed record ListJobsQuery : IRequest<Result<IReadOnlyList<JobResponse>>>;

public sealed record JobResponse(string Name, string Description, string Schedule, JobRunResponse? LastRun);

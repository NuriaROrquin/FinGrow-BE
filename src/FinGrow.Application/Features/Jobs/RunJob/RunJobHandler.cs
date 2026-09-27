namespace FinGrow.Application.Features.Jobs.RunJob;

using FinGrow.Application.Common;
using FinGrow.Application.Jobs;
using MediatR;

internal sealed class RunJobHandler : IRequestHandler<RunJobCommand, Result<JobRunResponse>>
{
    private readonly JobRunner _runner;

    public RunJobHandler(JobRunner runner) => _runner = runner;

    public async Task<Result<JobRunResponse>> Handle(RunJobCommand request, CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(request.JobName, cancellationToken);

        return result.IsSuccess
            ? Result.Success(JobRunResponse.From(result.Value))
            : Result.Failure<JobRunResponse>(result.Error);
    }
}

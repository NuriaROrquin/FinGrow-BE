namespace FinGrow.Application.Features.Jobs.RunJob;

using FinGrow.Application.Common;
using MediatR;

public sealed record RunJobCommand(string JobName) : IRequest<Result<JobRunResponse>>;

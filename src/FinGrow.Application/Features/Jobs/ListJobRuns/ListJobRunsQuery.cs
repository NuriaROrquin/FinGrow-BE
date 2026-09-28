namespace FinGrow.Application.Features.Jobs.ListJobRuns;

using FinGrow.Application.Common;
using FluentValidation;
using MediatR;

public sealed record ListJobRunsQuery(string JobName, int Take = ListJobRunsQuery.DefaultTake)
    : IRequest<Result<IReadOnlyList<JobRunResponse>>>
{
    public const int DefaultTake = 20;
    public const int MaxTake = 100;
}

public sealed class ListJobRunsQueryValidator : AbstractValidator<ListJobRunsQuery>
{
    public ListJobRunsQueryValidator()
    {
        RuleFor(query => query.JobName)
            .NotEmpty()
            .WithMessage("El nombre del trabajo es obligatorio.");

        RuleFor(query => query.Take)
            .InclusiveBetween(1, ListJobRunsQuery.MaxTake)
            .WithMessage($"Se pueden pedir entre 1 y {ListJobRunsQuery.MaxTake} corridas.");
    }
}

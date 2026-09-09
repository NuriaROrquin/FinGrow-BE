namespace FinGrow.Application.Common.Behaviors;

using System.Reflection;
using FluentValidation;
using MediatR;

internal sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private static readonly MethodInfo GenericFailureMethod = typeof(Result)
        .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!;

    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var failures = _validators
            .Select(validator => validator.Validate(request))
            .SelectMany(result => result.Errors)
            .Select(failure => failure.ErrorMessage)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var error = Error.Validation("Validation.General", string.Join(" ", failures));

        return CreateFailure(error);
    }

    private static TResponse CreateFailure(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = GenericFailureMethod.MakeGenericMethod(valueType);

        return (TResponse)failureMethod.Invoke(null, [error])!;
    }
}

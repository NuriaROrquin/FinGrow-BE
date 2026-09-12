namespace FinGrow.Application.UnitTests.Behaviors;

using FinGrow.Application.Common;
using FinGrow.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;

public class ValidationBehaviorTests
{
    private sealed record FakeRequest(string Name);

    private sealed class FakeValidator : AbstractValidator<FakeRequest>
    {
        public FakeValidator() => RuleFor(request => request.Name).NotEmpty();
    }

    [Fact]
    public async Task An_invalid_request_returns_failure_without_invoking_the_handler()
    {
        var behavior = new ValidationBehavior<FakeRequest, Result>([new FakeValidator()]);
        var handlerWasInvoked = false;

        var result = await behavior.Handle(
            new FakeRequest(string.Empty),
            _ =>
            {
                handlerWasInvoked = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        handlerWasInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task An_invalid_request_with_a_generic_response_returns_failure_of_the_correct_type()
    {
        var behavior = new ValidationBehavior<FakeRequest, Result<string>>([new FakeValidator()]);

        var result = await behavior.Handle(
            new FakeRequest(string.Empty),
            _ => Task.FromResult(Result.Success("value")),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task A_valid_request_invokes_the_handler()
    {
        var behavior = new ValidationBehavior<FakeRequest, Result>([new FakeValidator()]);
        var handlerWasInvoked = false;

        var result = await behavior.Handle(
            new FakeRequest("Cande"),
            _ =>
            {
                handlerWasInvoked = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        handlerWasInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task A_request_with_no_registered_validators_invokes_the_handler_directly()
    {
        var behavior = new ValidationBehavior<FakeRequest, Result>([]);
        var handlerWasInvoked = false;

        await behavior.Handle(
            new FakeRequest(string.Empty),
            _ =>
            {
                handlerWasInvoked = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        handlerWasInvoked.ShouldBeTrue();
    }
}

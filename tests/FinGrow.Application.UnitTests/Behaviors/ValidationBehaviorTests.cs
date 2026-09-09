namespace FinGrow.Application.UnitTests.Behaviors;

using FinGrow.Application.Common;
using FinGrow.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;

public class ValidationBehaviorTests
{
    private sealed record FakeRequest(string Nombre);

    private sealed class FakeValidator : AbstractValidator<FakeRequest>
    {
        public FakeValidator() => RuleFor(request => request.Nombre).NotEmpty();
    }

    [Fact]
    public async Task Un_request_invalido_devuelve_Failure_sin_invocar_al_handler()
    {
        var behavior = new ValidationBehavior<FakeRequest, Result>([new FakeValidator()]);
        var handlerFueInvocado = false;

        var result = await behavior.Handle(
            new FakeRequest(string.Empty),
            _ =>
            {
                handlerFueInvocado = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        handlerFueInvocado.ShouldBeFalse();
    }

    [Fact]
    public async Task Un_request_invalido_con_respuesta_generica_devuelve_Failure_del_tipo_correcto()
    {
        var behavior = new ValidationBehavior<FakeRequest, Result<string>>([new FakeValidator()]);

        var result = await behavior.Handle(
            new FakeRequest(string.Empty),
            _ => Task.FromResult(Result.Success("valor")),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Un_request_valido_invoca_al_handler()
    {
        var behavior = new ValidationBehavior<FakeRequest, Result>([new FakeValidator()]);
        var handlerFueInvocado = false;

        var result = await behavior.Handle(
            new FakeRequest("Cande"),
            _ =>
            {
                handlerFueInvocado = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        handlerFueInvocado.ShouldBeTrue();
    }

    [Fact]
    public async Task Un_request_sin_validadores_registrados_invoca_al_handler_directamente()
    {
        var behavior = new ValidationBehavior<FakeRequest, Result>([]);
        var handlerFueInvocado = false;

        await behavior.Handle(
            new FakeRequest(string.Empty),
            _ =>
            {
                handlerFueInvocado = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        handlerFueInvocado.ShouldBeTrue();
    }
}

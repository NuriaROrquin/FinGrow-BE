namespace FinGrow.Application.Features.Login;

using FluentValidation;

public sealed class VerifyTwoFactorLoginCommandValidator : AbstractValidator<VerifyTwoFactorLoginCommand>
{
    public VerifyTwoFactorLoginCommandValidator()
    {
        RuleFor(command => command.ChallengeToken)
            .NotEmpty()
            .WithMessage("Falta el token del ingreso.");

        RuleFor(command => command.Code)
            .NotEmpty()
            .WithMessage("El código de verificación es obligatorio.");
    }
}

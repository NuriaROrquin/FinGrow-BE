namespace FinGrow.Application.Features.Account.TwoFactor;

using FluentValidation;

public sealed class DisableTwoFactorCommandValidator : AbstractValidator<DisableTwoFactorCommand>
{
    public DisableTwoFactorCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .WithMessage("El código de verificación es obligatorio.");
    }
}

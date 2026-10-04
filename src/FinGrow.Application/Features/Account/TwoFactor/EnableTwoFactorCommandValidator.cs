namespace FinGrow.Application.Features.Account.TwoFactor;

using FluentValidation;

public sealed class EnableTwoFactorCommandValidator : AbstractValidator<EnableTwoFactorCommand>
{
    public EnableTwoFactorCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .WithMessage("El código de verificación es obligatorio.");
    }
}

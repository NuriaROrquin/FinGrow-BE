namespace FinGrow.Application.Features.PasswordReset.ResetPassword;

using FluentValidation;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty()
            .WithMessage("Falta el enlace para restablecer la contraseña.");

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("La nueva contraseña debe tener al menos 8 caracteres.");
    }
}

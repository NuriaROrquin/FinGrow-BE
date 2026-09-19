namespace FinGrow.Application.Features.Account.ChangePassword;

using FluentValidation;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .WithMessage("La contrasena actual es obligatoria.");

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("La nueva contrasena debe tener al menos 8 caracteres.");

        RuleFor(command => command.NewPassword)
            .NotEqual(command => command.CurrentPassword)
            .WithMessage("La nueva contrasena debe ser distinta de la actual.");
    }
}

namespace FinGrow.Application.Features.PasswordReset.RequestPasswordReset;

using FinGrow.Application.Common;
using FluentValidation;

public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("El email es obligatorio.");

        RuleFor(command => command.Role)
            .Must(role => role is Rol.Empleado or Rol.Empresa)
            .WithMessage("El tipo de cuenta no es valido.");
    }
}

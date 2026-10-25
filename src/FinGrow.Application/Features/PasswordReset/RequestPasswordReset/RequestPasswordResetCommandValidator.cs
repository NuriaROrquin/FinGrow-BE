namespace FinGrow.Application.Features.PasswordReset.RequestPasswordReset;

using FluentValidation;

public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("El email es obligatorio.");
    }
}

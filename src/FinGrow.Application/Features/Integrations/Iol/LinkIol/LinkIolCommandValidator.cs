namespace FinGrow.Application.Features.Integrations.Iol.LinkIol;

using FinGrow.Domain.Entities;
using FluentValidation;

public sealed class LinkIolCommandValidator : AbstractValidator<LinkIolCommand>
{
    public const int MaxPasswordLength = 200;

    public LinkIolCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty().WithMessage("El usuario de IOL es obligatorio.")
            .MaximumLength(EmployeeIntegration.MaxExternalAccountIdLength)
            .WithMessage($"El usuario de IOL no puede superar los {EmployeeIntegration.MaxExternalAccountIdLength} caracteres.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("La contraseña de IOL es obligatoria.")
            .MaximumLength(MaxPasswordLength)
            .WithMessage($"La contraseña de IOL no puede superar los {MaxPasswordLength} caracteres.");
    }
}

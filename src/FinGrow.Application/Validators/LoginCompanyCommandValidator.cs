namespace FinGrow.Application.Validators;

using FinGrow.Application.Features.Login;
using FluentValidation;

public sealed class LoginCompanyCommandValidator : AbstractValidator<LoginCompanyCommand>
{
    public LoginCompanyCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty();
        RuleFor(command => command.Password).NotEmpty();
    }
}

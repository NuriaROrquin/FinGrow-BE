namespace FinGrow.Application.Features.Account.Withdraw;

using FluentValidation;

public sealed class WithdrawAccountCommandValidator : AbstractValidator<WithdrawAccountCommand>
{
    public WithdrawAccountCommandValidator() =>
        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("Para darte de baja tenes que confirmar tu contrasena.");
}

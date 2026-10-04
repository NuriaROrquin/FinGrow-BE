namespace FinGrow.Application.Features.Account.Profile;

using FinGrow.Domain.Entities;
using FluentValidation;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.FullName)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.")
            .MaximumLength(Employee.MaxFullNameLength)
            .WithMessage($"El nombre no puede superar los {Employee.MaxFullNameLength} caracteres.");

        RuleFor(command => command.PhoneNumber)
            .MaximumLength(Employee.MaxPhoneNumberLength)
            .WithMessage($"El teléfono no puede superar los {Employee.MaxPhoneNumberLength} caracteres.");
    }
}

namespace FinGrow.Application.Features.Account.Profile;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FluentValidation;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(command => command.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.")
            .Must(fullName => fullName.Trim().Length >= Employee.MinFullNameLength)
            .WithMessage($"El nombre tiene que tener al menos {Employee.MinFullNameLength} caracteres.")
            .MaximumLength(Employee.MaxFullNameLength)
            .WithMessage($"El nombre no puede superar los {Employee.MaxFullNameLength} caracteres.");

        RuleFor(command => command.PhoneNumber)
            .Must(phoneNumber => Employee.IsValidPhoneNumber(phoneNumber!.Trim()))
            .When(command => !string.IsNullOrWhiteSpace(command.PhoneNumber))
            .WithMessage(
                $"El teléfono solo admite números, espacios, +, - y paréntesis, con entre {Employee.MinPhoneNumberDigits} y {Employee.MaxPhoneNumberDigits} dígitos.");

        RuleFor(command => command.NationalId)
            .Must(nationalId => Employee.IsValidNationalId(nationalId!.Trim()))
            .When(command => !string.IsNullOrWhiteSpace(command.NationalId))
            .WithMessage(
                $"El DNI tiene que tener entre {Employee.MinNationalIdLength} y {Employee.MaxNationalIdLength} números, sin puntos.");

        RuleFor(command => command.BirthDate)
            .Must(birthDate => Employee.IsValidBirthDate(birthDate!.Value, clock.Today))
            .When(command => command.BirthDate is not null)
            .WithMessage($"La fecha de nacimiento tiene que corresponder a una edad de entre {Employee.MinAge} y {Employee.MaxAge} años.");

        RuleFor(command => command.Address)
            .MaximumLength(Employee.MaxAddressLength)
            .WithMessage($"La dirección no puede superar los {Employee.MaxAddressLength} caracteres.");
    }
}

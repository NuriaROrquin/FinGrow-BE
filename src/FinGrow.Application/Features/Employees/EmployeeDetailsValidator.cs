namespace FinGrow.Application.Features.Employees;

using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using FluentValidation;

internal interface IEmployeeDetails
{
    string FullName { get; }

    string Email { get; }

    string? PhoneNumber { get; }
}

internal sealed class EmployeeDetailsValidator : AbstractValidator<IEmployeeDetails>
{
    public EmployeeDetailsValidator()
    {
        RuleFor(details => details.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("El nombre es obligatorio.")
            .Must(fullName => fullName.Trim().Length >= Employee.MinFullNameLength)
            .WithMessage($"El nombre tiene que tener al menos {Employee.MinFullNameLength} caracteres.")
            .Must(fullName => fullName.Trim().Length <= Employee.MaxFullNameLength)
            .WithMessage($"El nombre no puede superar los {Employee.MaxFullNameLength} caracteres.");

        RuleFor(details => details.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("El email es obligatorio.")
            .Must(Email.IsValid)
            .WithMessage("El email no es una dirección válida.");

        RuleFor(details => details.PhoneNumber)
            .Must(phoneNumber => Employee.IsValidPhoneNumber(phoneNumber!.Trim()))
            .When(details => !string.IsNullOrWhiteSpace(details.PhoneNumber))
            .WithMessage(
                $"El teléfono solo admite números, espacios, +, - y paréntesis, con entre {Employee.MinPhoneNumberDigits} y {Employee.MaxPhoneNumberDigits} dígitos.");
    }
}

namespace FinGrow.Application.Features.CompanyProfile.UpdateCompanyProfile;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;
using FluentValidation;

public sealed class UpdateCompanyProfileCommandValidator : AbstractValidator<UpdateCompanyProfileCommand>
{
    public UpdateCompanyProfileCommandValidator()
    {
        RuleFor(command => command.LegalName)
            .NotEmpty()
            .WithMessage("La razón social es obligatoria.")
            .MaximumLength(Company.MaxNameLength)
            .WithMessage($"La razón social no puede superar los {Company.MaxNameLength} caracteres.");

        RuleFor(command => command.TaxId)
            .Must(IsValidTaxId)
            .WithMessage("El CUIT no es válido.");

        RuleFor(command => command.Email)
            .Must(Email.IsValid)
            .WithMessage("El email no es válido.");

        RuleFor(command => command.Industry)
            .MaximumLength(Company.MaxIndustryLength)
            .WithMessage($"La industria no puede superar los {Company.MaxIndustryLength} caracteres.")
            .Must(industry => industry is null || IndustryTypes.IsValid(industry))
            .WithMessage("La industria seleccionada no es válida.");

        RuleFor(command => command.CorporatePhone)
            .MaximumLength(Company.MaxPhoneNumberLength)
            .WithMessage($"El teléfono no puede superar los {Company.MaxPhoneNumberLength} caracteres.");

        RuleFor(command => command.FiscalAddress)
            .MaximumLength(Company.MaxFiscalAddressLength)
            .WithMessage($"La dirección fiscal no puede superar los {Company.MaxFiscalAddressLength} caracteres.");

        RuleFor(command => command.FoundedYear)
            .InclusiveBetween(1800, 9999)
            .When(command => command.FoundedYear is not null)
            .WithMessage("El año de fundación no es válido.");
    }

    private static bool IsValidTaxId(string value)
    {
        try
        {
            TaxId.From(value);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }
}

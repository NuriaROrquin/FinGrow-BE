namespace FinGrow.Application.Features.CompanyPreferences.UpdateCompanyPreferences;

using FluentValidation;

public sealed class UpdateCompanyPreferencesCommandValidator
    : AbstractValidator<UpdateCompanyPreferencesCommand>
{
    public UpdateCompanyPreferencesCommandValidator()
    {
        RuleFor(command => command.Theme)
            .IsInEnum()
            .WithMessage("El tema no es válido.");

        RuleFor(command => command.Language)
            .IsInEnum()
            .WithMessage("El idioma no es válido.");

        RuleFor(command => command.Currency)
            .IsInEnum()
            .WithMessage("La moneda no es válida.");

        RuleFor(command => command.FiscalYearStartMonth)
            .InclusiveBetween(1, 12)
            .WithMessage("El mes de inicio del año fiscal tiene que estar entre 1 y 12.");
    }
}

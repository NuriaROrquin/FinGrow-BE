namespace FinGrow.Application.Features.Account.Preferences;

using FluentValidation;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
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

        RuleFor(command => command.DateFormat)
            .IsInEnum()
            .WithMessage("El formato de fecha no es válido.");
    }
}

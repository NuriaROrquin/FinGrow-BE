namespace FinGrow.Application.Features.Integrations.GenerateLinkCode;

using FinGrow.Domain.Enums;
using FluentValidation;

public sealed class GenerateLinkCodeValidator : AbstractValidator<GenerateLinkCodeCommand>
{
    public GenerateLinkCodeValidator()
    {
        RuleFor(command => command.Provider)
            .IsInEnum()
            .Must(provider => provider.LinksWithCode())
            .WithMessage("Esa integracion no se vincula con un codigo.");
    }
}

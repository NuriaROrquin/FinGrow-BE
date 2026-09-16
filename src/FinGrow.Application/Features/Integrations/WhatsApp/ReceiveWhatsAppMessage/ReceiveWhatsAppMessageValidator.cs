namespace FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;

using FluentValidation;

public sealed class ReceiveWhatsAppMessageValidator : AbstractValidator<ReceiveWhatsAppMessageCommand>
{
    public ReceiveWhatsAppMessageValidator()
    {
        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+[1-9][0-9]{6,14}$")
            .WithMessage("El remitente tiene que ser un numero en formato E.164 (+<codigo de pais><numero>).");

        RuleFor(command => command.MessageSid).NotEmpty();
        RuleFor(command => command.Media).NotNull();
    }
}

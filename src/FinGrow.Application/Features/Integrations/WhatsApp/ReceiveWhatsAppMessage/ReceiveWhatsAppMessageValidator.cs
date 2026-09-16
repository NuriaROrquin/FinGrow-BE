namespace FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;

using FluentValidation;

public sealed class ReceiveWhatsAppMessageValidator : AbstractValidator<ReceiveWhatsAppMessageCommand>
{
    public ReceiveWhatsAppMessageValidator()
    {
        RuleFor(command => command.From)
            .NotEmpty()
            .Must(WhatsAppAddress.IsValid)
            .WithMessage("El remitente tiene que venir como 'whatsapp:+<numero en E.164>'.");

        RuleFor(command => command.MessageSid).NotEmpty();
        RuleFor(command => command.Media).NotNull();
    }
}

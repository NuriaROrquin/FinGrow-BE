namespace FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;

using FluentValidation;

public sealed class ReceiveTelegramMessageValidator : AbstractValidator<ReceiveTelegramMessageCommand>
{
    public ReceiveTelegramMessageValidator()
    {
        RuleFor(command => command.ChatId).NotEqual(0);
        RuleFor(command => command.Text).NotNull();
        RuleFor(command => command.VoiceNote!.FileId).NotEmpty().When(command => command.VoiceNote is not null);
        RuleFor(command => command.Receipt!.FileId).NotEmpty().When(command => command.Receipt is not null);
    }
}

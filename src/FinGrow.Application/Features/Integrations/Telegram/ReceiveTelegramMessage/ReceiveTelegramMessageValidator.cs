namespace FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;

using FluentValidation;

public sealed class ReceiveTelegramMessageValidator : AbstractValidator<ReceiveTelegramMessageCommand>
{
    public ReceiveTelegramMessageValidator()
    {
        RuleFor(command => command.ChatId).NotEqual(0);
        RuleFor(command => command.Text).NotNull();
    }
}

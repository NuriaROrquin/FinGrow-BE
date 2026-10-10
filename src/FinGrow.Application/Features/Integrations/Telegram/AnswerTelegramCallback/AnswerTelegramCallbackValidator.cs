namespace FinGrow.Application.Features.Integrations.Telegram.AnswerTelegramCallback;

using FluentValidation;

public sealed class AnswerTelegramCallbackValidator : AbstractValidator<AnswerTelegramCallbackCommand>
{
    public AnswerTelegramCallbackValidator()
    {
        RuleFor(command => command.ChatId).NotEqual(0);
        RuleFor(command => command.CallbackQueryId).NotEmpty();
        RuleFor(command => command.Data).NotNull();
    }
}

namespace FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;

using System.Globalization;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class ReceiveTelegramMessageHandler : IRequestHandler<ReceiveTelegramMessageCommand, Result>
{
    internal const string StartCommand = "/start";

    internal const string NotLinkedReply =
        "Este chat todavía no está vinculado a ninguna cuenta de FinGrow. " +
        "Generá un código desde la app (Integraciones → Telegram) y envialo por acá.";

    internal const string InvalidCodeReply =
        "Ese código no es válido o ya venció. Generá uno nuevo desde la app y volvé a intentar.";

    internal const string LinkedReplySuffix = ": este Telegram quedó vinculado a tu cuenta de FinGrow.";

    internal const string MessageReceivedReply =
        "Recibido. Todavía no registro movimientos por Telegram, pero tu chat ya está vinculado.";

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly LinkCodeRedeemer _linker;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<ReceiveTelegramMessageHandler> _logger;

    public ReceiveTelegramMessageHandler(
        IEmployeeIntegrationRepository integrations,
        LinkCodeRedeemer linker,
        ITelegramBotClient bot,
        ILogger<ReceiveTelegramMessageHandler> logger)
    {
        _integrations = integrations;
        _linker = linker;
        _bot = bot;
        _logger = logger;
    }

    public async Task<Result> Handle(ReceiveTelegramMessageCommand request, CancellationToken cancellationToken)
    {
        var chatId = request.ChatId.ToString(CultureInfo.InvariantCulture);
        var integration = await _integrations.FindByExternalAccountAsync(
            IntegrationProvider.Telegram, chatId, cancellationToken);

        var reply = integration is null
            ? await TryLinkAsync(chatId, request.Text, cancellationToken)
            : HandleLinkedMessage(integration.EmployeeId, request);

        await ReplyAsync(request.ChatId, reply, cancellationToken);

        return Result.Success();
    }

    internal static string StripStartCommand(string text)
    {
        var trimmed = text.Trim();

        return trimmed.StartsWith(StartCommand, StringComparison.OrdinalIgnoreCase)
            ? trimmed[StartCommand.Length..]
            : trimmed;
    }

    private async Task<string> TryLinkAsync(string chatId, string text, CancellationToken cancellationToken)
    {
        var attempt = await _linker.TryLinkAsync(
            IntegrationProvider.Telegram, chatId, StripStartCommand(text), cancellationToken);

        return attempt.Outcome switch
        {
            LinkOutcome.NotACode => NotLinkedReply,
            LinkOutcome.InvalidCode => InvalidCodeReply,
            _ => $"Listo, {attempt.Employee!.FullName}{LinkedReplySuffix}"
        };
    }

    private string HandleLinkedMessage(Guid employeeId, ReceiveTelegramMessageCommand request)
    {
        LogMessage(_logger, employeeId, request.Text.Length, request.MessageId);

        return MessageReceivedReply;
    }

    private async Task ReplyAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        try
        {
            await _bot.SendMessageAsync(chatId, text, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogReplyFailed(_logger, exception, chatId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogReplyFailed(_logger, exception, chatId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensaje de Telegram del empleado {EmployeeId}: {Characters} caracteres (mensaje {MessageId}).")]
    private static partial void LogMessage(ILogger logger, Guid employeeId, int characters, long messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo responder al chat de Telegram {ChatId}.")]
    private static partial void LogReplyFailed(ILogger logger, Exception exception, long chatId);
}

namespace FinGrow.Application.Features.Integrations.WhatsApp;

using FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;
using FinGrow.Application.Interfaces;
using Microsoft.Extensions.Logging;

internal enum VoiceNoteOutcome
{
    Transcribed,
    NotUnderstood,
    Unavailable
}

internal sealed record VoiceNote(VoiceNoteOutcome Outcome, string? Text = null)
{
    public static VoiceNote Of(VoiceNoteOutcome outcome) => new(outcome);
}

internal sealed partial class WhatsAppVoiceNoteTranscriber
{
    private readonly ITwilioMediaClient _media;
    private readonly IAiService _ai;
    private readonly ILogger<WhatsAppVoiceNoteTranscriber> _logger;

    public WhatsAppVoiceNoteTranscriber(ITwilioMediaClient media, IAiService ai, ILogger<WhatsAppVoiceNoteTranscriber> logger)
    {
        _media = media;
        _ai = ai;
        _logger = logger;
    }

    public static bool IsVoiceNote(WhatsAppInboundMedia attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        return attachment.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<VoiceNote> TranscribeAsync(Guid employeeId, WhatsAppInboundMedia attachment, CancellationToken cancellationToken)
    {
        try
        {
            var media = await _media.DownloadAsync(attachment.Url, cancellationToken);
            var transcription = await _ai.TranscribeAudioAsync(media.Content, attachment.ContentType, cancellationToken);

            LogTranscription(_logger, employeeId, media.Content.Length, transcription.Outcome);

            return transcription is { Outcome: AudioTranscriptionOutcome.Transcribed, Text: { } text } && !string.IsNullOrWhiteSpace(text)
                ? new VoiceNote(VoiceNoteOutcome.Transcribed, text)
                : VoiceNote.Of(VoiceNoteOutcome.NotUnderstood);
        }
        catch (HttpRequestException exception)
        {
            LogUnavailable(_logger, exception, employeeId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogUnavailable(_logger, exception, employeeId);
        }

        return VoiceNote.Of(VoiceNoteOutcome.Unavailable);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Nota de voz de WhatsApp del empleado {EmployeeId}: {Bytes} bytes, transcripcion {Outcome}.")]
    private static partial void LogTranscription(ILogger logger, Guid employeeId, int bytes, AudioTranscriptionOutcome outcome);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo descargar o transcribir la nota de voz del empleado {EmployeeId}; no se registro ningun movimiento.")]
    private static partial void LogUnavailable(ILogger logger, Exception exception, Guid employeeId);
}

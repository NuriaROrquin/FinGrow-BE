namespace FinGrow.Application.Features.Integrations.ChatTransactions;

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

internal sealed partial class ChatVoiceNoteTranscriber
{
    private readonly IAiService _ai;
    private readonly ILogger<ChatVoiceNoteTranscriber> _logger;

    public ChatVoiceNoteTranscriber(IAiService ai, ILogger<ChatVoiceNoteTranscriber> logger)
    {
        _ai = ai;
        _logger = logger;
    }

    public async Task<VoiceNote> TranscribeAsync(
        Guid employeeId,
        Func<CancellationToken, Task<ReadOnlyMemory<byte>>> download,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(download);

        try
        {
            var audio = await download(cancellationToken);
            var transcription = await _ai.TranscribeAudioAsync(audio, contentType, cancellationToken);

            LogTranscription(_logger, employeeId, audio.Length, transcription.Outcome);

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Nota de voz del empleado {EmployeeId}: {Bytes} bytes, transcripcion {Outcome}.")]
    private static partial void LogTranscription(ILogger logger, Guid employeeId, int bytes, AudioTranscriptionOutcome outcome);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo descargar o transcribir la nota de voz del empleado {EmployeeId}; no se registro ningun movimiento.")]
    private static partial void LogUnavailable(ILogger logger, Exception exception, Guid employeeId);
}

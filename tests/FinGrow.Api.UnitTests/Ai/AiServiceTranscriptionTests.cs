namespace FinGrow.Api.UnitTests.Ai;

using System.Net;
using System.Text;
using FinGrow.Application.Interfaces;
using FinGrow.Infrastructure.Ai;

public class AiServiceTranscriptionTests
{
    private static readonly byte[] VoiceNote = Encoding.ASCII.GetBytes("OggS-voice-note");

    [Fact]
    public async Task The_audio_is_sent_as_the_file_part_of_a_multipart_request()
    {
        using var handler = new RecordingHandler("""{"text": "gasté 500", "language": "es", "model": "m"}""");

        await Transcribe(handler, "audio/ogg; codecs=opus");

        handler.Path.ShouldBe("/api/v1/transcription/audio");
        handler.PartName.ShouldBe("file");
        handler.PartContentType.ShouldBe("audio/ogg; codecs=opus");
        handler.PartBytes.ShouldBe(VoiceNote);
    }

    [Fact]
    public async Task A_content_type_that_cannot_be_parsed_is_sent_as_octet_stream()
    {
        using var handler = new RecordingHandler("""{"text": "gasté 500", "language": "es", "model": "m"}""");

        await Transcribe(handler, "no es un content type");

        handler.PartContentType.ShouldBe("application/octet-stream");
    }

    [Fact]
    public async Task A_transcription_returns_the_trimmed_text_and_the_model()
    {
        using var handler = new RecordingHandler("""{"text": "  Gasté 2500 pesos en el súper  ", "language": "es", "model": "gpt-4o-mini-transcribe"}""");

        var transcription = await Transcribe(handler);

        transcription.ShouldBe(new AudioTranscription(
            AudioTranscriptionOutcome.Transcribed, "Gasté 2500 pesos en el súper", "gpt-4o-mini-transcribe"));
    }

    [Theory]
    [InlineData("audio_not_understood", HttpStatusCode.UnprocessableEntity, AudioTranscriptionOutcome.NotUnderstood)]
    [InlineData("unsupported_audio", HttpStatusCode.UnsupportedMediaType, AudioTranscriptionOutcome.UnsupportedFormat)]
    [InlineData("audio_too_large", HttpStatusCode.RequestEntityTooLarge, AudioTranscriptionOutcome.TooLarge)]
    public async Task An_audio_that_cannot_be_transcribed_is_an_expected_outcome(
        string code,
        HttpStatusCode status,
        AudioTranscriptionOutcome expected)
    {
        using var handler = new RecordingHandler($$"""{"code": "{{code}}", "message": "x"}""", status);

        var transcription = await Transcribe(handler);

        transcription.ShouldBe(AudioTranscription.Of(expected));
    }

    [Theory]
    [InlineData("""{"code": "model_unavailable", "message": "caido"}""", HttpStatusCode.ServiceUnavailable)]
    [InlineData("""{"code": "validation_error", "message": "falta el archivo"}""", HttpStatusCode.UnprocessableEntity)]
    [InlineData("<html>Bad Gateway</html>", HttpStatusCode.BadGateway)]
    public async Task Any_other_error_from_FinGrow_AI_is_an_http_failure(string body, HttpStatusCode status)
    {
        using var handler = new RecordingHandler(body, status);

        await Should.ThrowAsync<HttpRequestException>(() => Transcribe(handler));
    }

    [Theory]
    [InlineData("""{"text": "   ", "language": "es", "model": "m"}""")]
    [InlineData("""{"language": "es", "model": "m"}""")]
    [InlineData("esto no es json")]
    public async Task A_successful_answer_without_text_is_an_http_failure(string body)
    {
        using var handler = new RecordingHandler(body);

        await Should.ThrowAsync<HttpRequestException>(() => Transcribe(handler));
    }

    private static async Task<AudioTranscription> Transcribe(RecordingHandler handler, string contentType = "audio/ogg")
    {
        using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://fingrow-ai.test") };

        return await new AiService(httpClient).TranscribeAudioAsync(VoiceNote, contentType);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _status;

        public RecordingHandler(string response, HttpStatusCode status = HttpStatusCode.OK)
        {
            _response = response;
            _status = status;
        }

        public string? Path { get; private set; }

        public string? PartName { get; private set; }

        public string? PartContentType { get; private set; }

        public byte[]? PartBytes { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;

            if (request.Content is MultipartFormDataContent form)
            {
                var part = form.Single();
                PartName = part.Headers.ContentDisposition?.Name?.Trim('"');
                PartContentType = part.Headers.ContentType?.ToString();
                PartBytes = await part.ReadAsByteArrayAsync(cancellationToken);
            }

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            };
        }
    }
}

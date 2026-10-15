namespace FinGrow.Api.UnitTests.Integrations;

using System.Net;
using System.Text;
using FinGrow.Infrastructure.Integrations.Telegram;

public class TelegramBotClientTests
{
    private static readonly Uri BotApi = new("https://api.telegram.org/botunit-test-token/");

    [Fact]
    public async Task A_file_is_located_with_get_file_and_downloaded_from_the_file_endpoint()
    {
        using var handler = new BotApiHandler("""{"ok": true, "result": {"file_id": "AwAC", "file_path": "voice/file_7.oga"}}""");
        using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = BotApi };

        var content = await new TelegramBotClient(httpClient).DownloadFileAsync("AwAC");

        content.ToArray().ShouldBe(BotApiHandler.FileContent);
        handler.Requested.ShouldBe(new[]
        {
            new Uri("https://api.telegram.org/botunit-test-token/getFile?file_id=AwAC"),
            new Uri("https://api.telegram.org/file/botunit-test-token/voice/file_7.oga"),
        });
    }

    [Fact]
    public async Task A_file_telegram_cannot_locate_is_an_http_failure()
    {
        using var handler = new BotApiHandler("""{"ok": false, "description": "Bad Request: invalid file_id"}""");
        using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = BotApi };

        await Should.ThrowAsync<HttpRequestException>(() => new TelegramBotClient(httpClient).DownloadFileAsync("nope"));
    }

    private sealed class BotApiHandler : HttpMessageHandler
    {
        public static readonly byte[] FileContent = Encoding.ASCII.GetBytes("OggS-voice-note");

        private readonly string _getFileResponse;

        public BotApiHandler(string getFileResponse) => _getFileResponse = getFileResponse;

        public List<Uri> Requested { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            Requested.Add(uri);

            HttpContent content = uri.AbsolutePath.EndsWith("/getFile", StringComparison.Ordinal)
                ? new StringContent(_getFileResponse, Encoding.UTF8, "application/json")
                : new ByteArrayContent(FileContent);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}

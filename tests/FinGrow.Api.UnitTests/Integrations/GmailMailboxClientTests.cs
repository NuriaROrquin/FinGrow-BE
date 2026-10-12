namespace FinGrow.Api.UnitTests.Integrations;

using System.Net;
using System.Text;
using FinGrow.Infrastructure.Integrations.Gmail;
using Microsoft.AspNetCore.WebUtilities;

public class GmailMailboxClientTests
{
    [Fact]
    public async Task Searching_sends_the_query_with_the_access_token_and_reads_the_next_page()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """
            {"messages":[{"id":"m2","threadId":"t"},{"id":"m1","threadId":"t"}],"nextPageToken":"page-2"}
            """);

        var page = await CreateClient(handler).SearchAsync("ya29.access", "factura after:1", pageToken: "page-1");

        handler.RequestUri!.AbsolutePath.ShouldBe("/gmail/v1/users/me/messages");
        var query = QueryHelpers.ParseQuery(handler.RequestUri.Query);
        query["q"].ToString().ShouldBe("factura after:1");
        query["pageToken"].ToString().ShouldBe("page-1");
        handler.Authorization.ShouldBe("Bearer ya29.access");
        page.MessageIds.ShouldBe(new List<string> { "m2", "m1" });
        page.NextPageToken.ShouldBe("page-2");
    }

    [Fact]
    public async Task An_empty_search_has_no_messages_and_no_next_page()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"resultSizeEstimate":0}""");

        var page = await CreateClient(handler).SearchAsync("token", "q", pageToken: null);

        page.MessageIds.ShouldBeEmpty();
        page.NextPageToken.ShouldBeNull();
    }

    [Fact]
    public async Task A_message_is_read_with_its_date_subject_sender_domain_and_text()
    {
        var html = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("<p>TOTAL A PAGAR $73.456,92</p>"));
        var handler = new StubHandler(HttpStatusCode.OK, $$"""
            {"id":"m1","internalDate":"1791900000000","payload":{
              "mimeType":"text/html","filename":"",
              "headers":[{"name":"Subject","value":"Tu factura"},{"name":"From","value":"Edenor <facturas@edenor.com.ar>"}],
              "body":{"size":30,"data":"{{html}}"} } }
            """);

        var message = await CreateClient(handler).GetMessageAsync("token", "m1");

        handler.RequestUri!.PathAndQuery.ShouldBe("/gmail/v1/users/me/messages/m1?format=full");
        message.Id.ShouldBe("m1");
        message.Subject.ShouldBe("Tu factura");
        message.SenderDomain.ShouldBe("edenor.com.ar");
        message.ReceivedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1791900000000));
        message.Body.ShouldBe("TOTAL A PAGAR $73.456,92");
    }

    [Fact]
    public async Task A_rejected_token_surfaces_as_unauthorized()
    {
        var handler = new StubHandler(HttpStatusCode.Unauthorized, """{"error":{"code":401}}""");

        var exception = await Should.ThrowAsync<HttpRequestException>(
            () => CreateClient(handler).SearchAsync("token", "q", pageToken: null));

        exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static GmailMailboxClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://gmail.googleapis.com/") });

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _response;

        public StubHandler(HttpStatusCode status, string response)
        {
            _status = status;
            _response = response;
        }

        public Uri? RequestUri { get; private set; }

        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();

            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            });
        }
    }
}

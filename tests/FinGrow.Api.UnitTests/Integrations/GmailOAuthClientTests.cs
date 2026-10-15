namespace FinGrow.Api.UnitTests.Integrations;

using System.Net;
using System.Text;
using FinGrow.Infrastructure.Integrations.Gmail;
using Microsoft.AspNetCore.WebUtilities;

public class GmailOAuthClientTests
{
    private static readonly GmailOptions Options = new()
    {
        ClientId = "client-id.apps.googleusercontent.com",
        ClientSecret = "client-secret",
        RedirectUri = "http://localhost:5210/api/integrations/gmail/oauth/callback",
    };

    [Fact]
    public void The_authorization_url_asks_for_offline_read_only_access_and_forces_the_consent_screen()
    {
        var client = CreateClient(new StubHandler());

        var url = client.BuildAuthorizationUrl("ABCD2345");

        url.GetLeftPart(UriPartial.Path).ShouldBe("https://accounts.google.com/o/oauth2/v2/auth");
        var query = QueryHelpers.ParseQuery(url.Query);
        query["client_id"].ToString().ShouldBe(Options.ClientId);
        query["redirect_uri"].ToString().ShouldBe(Options.RedirectUri);
        query["response_type"].ToString().ShouldBe("code");
        query["scope"].ToString().ShouldBe(GmailOAuthClient.ReadOnlyScope);
        query["access_type"].ToString().ShouldBe("offline");
        query["prompt"].ToString().ShouldBe("consent");
        query["state"].ToString().ShouldBe("ABCD2345");
    }

    [Fact]
    public async Task Exchanging_the_code_posts_a_form_and_reads_the_granted_scopes()
    {
        var handler = new StubHandler(HttpStatusCode.OK, $$"""
            {"access_token":"ya29.access","refresh_token":"1//refresh","expires_in":3599,
             "scope":"{{GmailOAuthClient.ReadOnlyScope}}","token_type":"Bearer"}
            """);
        var client = CreateClient(handler);

        var tokens = await client.ExchangeCodeAsync("4/code");

        tokens.AccessToken.ShouldBe("ya29.access");
        tokens.RefreshToken.ShouldBe("1//refresh");
        tokens.ExpiresIn.ShouldBe(TimeSpan.FromSeconds(3599));
        tokens.CanReadMail.ShouldBeTrue();
        handler.RequestUri.ShouldBe(new Uri("https://oauth2.googleapis.com/token"));
        handler.ContentType.ShouldBe("application/x-www-form-urlencoded");
        var form = QueryHelpers.ParseQuery(handler.Body);
        form["grant_type"].ToString().ShouldBe("authorization_code");
        form["code"].ToString().ShouldBe("4/code");
        form["client_secret"].ToString().ShouldBe(Options.ClientSecret);
        form["redirect_uri"].ToString().ShouldBe(Options.RedirectUri);
    }

    [Fact]
    public async Task A_token_without_the_read_scope_cannot_read_mail()
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.OK, """
            {"access_token":"ya29.access","refresh_token":"1//refresh","expires_in":3599,"scope":"openid"}
            """));

        var tokens = await client.ExchangeCodeAsync("4/code");

        tokens.CanReadMail.ShouldBeFalse();
    }

    [Fact]
    public async Task A_rejected_code_is_reported_as_an_http_failure()
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}"""));

        var exception = await Should.ThrowAsync<HttpRequestException>(() => client.ExchangeCodeAsync("4/used"));

        exception.Message.ShouldContain("invalid_grant");
    }

    [Fact]
    public async Task The_mailbox_address_comes_from_the_gmail_profile()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"emailAddress":"juan.perez@gmail.com","messagesTotal":10}""");
        var client = CreateClient(handler);

        var email = await client.GetEmailAddressAsync("ya29.access");

        email.ShouldBe("juan.perez@gmail.com");
        handler.RequestUri.ShouldBe(new Uri("https://gmail.googleapis.com/gmail/v1/users/me/profile"));
        handler.Authorization.ShouldBe("Bearer ya29.access");
    }

    [Fact]
    public async Task Revoking_a_token_that_google_no_longer_knows_is_not_an_error()
    {
        var handler = new StubHandler(HttpStatusCode.BadRequest, """{"error":"invalid_token"}""");
        var client = CreateClient(handler);

        await client.RevokeAsync("1//refresh");

        handler.RequestUri.ShouldBe(new Uri("https://oauth2.googleapis.com/revoke"));
        QueryHelpers.ParseQuery(handler.Body)["token"].ToString().ShouldBe("1//refresh");
    }

    private static GmailOAuthClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri(Options.OAuthBaseUrl) }, Microsoft.Extensions.Options.Options.Create(Options));

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _response;

        public StubHandler(HttpStatusCode status = HttpStatusCode.OK, string response = "{}")
        {
            _status = status;
            _response = response;
        }

        public Uri? RequestUri { get; private set; }

        public string Body { get; private set; } = string.Empty;

        public string? ContentType { get; private set; }

        public string? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();

            if (request.Content is not null)
            {
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
                ContentType = request.Content.Headers.ContentType?.MediaType;
            }

            return new HttpResponseMessage(_status) { Content = new StringContent(_response, Encoding.UTF8, "application/json") };
        }
    }
}

namespace FinGrow.Api.UnitTests.Articles;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class ArticlesEndpointsTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Every_query_parameter_reaches_the_filters()
    {
        using var factory = new ArticlesWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/articles?category=Savings&maxReadingTime=5", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Articles.WasQueried.ShouldBeTrue();
        factory.Articles.RequestedCategory.ShouldBe(EducationCategory.Savings);
        factory.Articles.RequestedMaxReadingTimeMinutes.ShouldBe(5);
    }

    [Fact]
    public async Task The_list_shows_category_and_reading_time_without_the_body()
    {
        using var factory = new ArticlesWebApplicationFactory();
        factory.Articles.Articles.Add(PublishedArticle("fondo-de-emergencia"));
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/articles", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().ShouldBe(1);
        var item = body[0];
        item.GetProperty("slug").GetString().ShouldBe("fondo-de-emergencia");
        item.GetProperty("category").GetString().ShouldBe("Savings");
        item.GetProperty("readingTimeMinutes").GetInt32().ShouldBe(4);
        item.TryGetProperty("content", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Opening_an_article_returns_its_markdown_body()
    {
        using var factory = new ArticlesWebApplicationFactory();
        factory.Articles.Articles.Add(PublishedArticle("fondo-de-emergencia"));
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/articles/fondo-de-emergencia", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("slug").GetString().ShouldBe("fondo-de-emergencia");
        body.GetProperty("content").GetString().ShouldBe("## Por qué\n\nCubre **tres meses** de gastos.");
    }

    [Fact]
    public async Task An_article_that_does_not_exist_is_404()
    {
        using var factory = new ArticlesWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/articles/no-existe", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().ShouldBe("Article.NotFound");
    }

    [Theory]
    [InlineData("maxReadingTime=0")]
    [InlineData("category=Astrologia")]
    public async Task An_invalid_filter_is_rejected_with_400_without_querying_the_database(string query)
    {
        using var factory = new ArticlesWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri($"/api/articles?{query}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Articles.WasQueried.ShouldBeFalse();
    }

    [Fact]
    public async Task Without_a_session_the_articles_are_not_served()
    {
        using var factory = new ArticlesWebApplicationFactory();

        var response = await factory.CreateClient().GetAsync(new Uri("/api/articles", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Articles.WasQueried.ShouldBeFalse();
    }

    [Fact]
    public async Task A_company_session_cannot_list_the_articles()
    {
        using var factory = new ArticlesWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empresa);

        var response = await client.GetAsync(new Uri("/api/articles", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        factory.Articles.WasQueried.ShouldBeFalse();
    }

    private static Article PublishedArticle(string slug)
    {
        var article = Article.Create(
            slug,
            "Fondo de emergencia",
            "Cuánto guardar y dónde.",
            "## Por qué\n\nCubre **tres meses** de gastos.",
            EducationCategory.Savings,
            readingTimeMinutes: 4,
            relatedInvestmentType: null,
            Now);
        article.Publish(Now);

        return article;
    }

    private static HttpClient AuthenticatedClient(ArticlesWebApplicationFactory factory, string role)
    {
        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(Guid.CreateVersion7(), Guid.CreateVersion7(), role, "Ana Gomez");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class CapturingArticleRepository : IArticleRepository
    {
        public List<Article> Articles { get; } = new();

        public bool WasQueried { get; private set; }

        public EducationCategory? RequestedCategory { get; private set; }

        public int? RequestedMaxReadingTimeMinutes { get; private set; }

        public Task<IReadOnlyList<Article>> ListPublishedAsync(
            EducationCategory? category,
            int? maxReadingTimeMinutes,
            CancellationToken cancellationToken = default)
        {
            WasQueried = true;
            RequestedCategory = category;
            RequestedMaxReadingTimeMinutes = maxReadingTimeMinutes;

            return Task.FromResult<IReadOnlyList<Article>>(Articles.ToList());
        }

        public Task<Article?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(Articles.FirstOrDefault(article => article.Slug == slug));
    }

    private sealed class ArticlesWebApplicationFactory : WebApplicationFactory<Program>
    {
        public CapturingArticleRepository Articles { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
                    ["AiService:ApiKey"] = "unit-test-ai-api-key",
                    ["Twilio:AccountSid"] = "ACunit-test",
                    ["Twilio:AuthToken"] = "unit-test-twilio-auth-token",
                    ["Telegram:BotToken"] = "unit-test-telegram-bot-token",
                    ["Telegram:WebhookSecret"] = "unit-test-telegram-secret",
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key-1234",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IArticleRepository>();
                services.AddSingleton<IArticleRepository>(Articles);
            });
        }
    }
}

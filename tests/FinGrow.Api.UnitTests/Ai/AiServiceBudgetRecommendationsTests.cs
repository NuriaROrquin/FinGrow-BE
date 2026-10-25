namespace FinGrow.Api.UnitTests.Ai;

using System.Net;
using System.Text;
using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Infrastructure.Ai;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public class AiServiceBudgetRecommendationsTests
{
    private const string ThreeRecommendations = """
        {
          "recommendations": [
            {"kind": "positive", "title": "Transporte en orden", "body": "Respetaste el tope.", "action": "budgets"},
            {"kind": "warning", "title": "Alimentos", "body": "Te pasaste dos meses.", "action": "transactions"},
            {"kind": "tip", "title": "Entretenimiento", "body": "Ponele un tope.", "action": "budgets"}
          ],
          "model": "m"
        }
        """;

    private static readonly BudgetRecommendationsInput Input = new(
        Language.en,
        Currency.USD,
        new[]
        {
            new BudgetRecommendationMonth(
                2026,
                9,
                false,
                new[]
                {
                    new RecommendationBudgetLimit(ExpenseCategory.Transporte, 100m, 80m),
                    new RecommendationBudgetLimit(ExpenseCategory.Alimentos, 300m, 350m),
                },
                new Dictionary<ExpenseCategory, decimal>
                {
                    [ExpenseCategory.Entretenimiento] = 90m,
                    [ExpenseCategory.Salud] = 0m,
                }),
            new BudgetRecommendationMonth(
                2026,
                10,
                true,
                new[] { new RecommendationBudgetLimit(ExpenseCategory.Alimentos, 300m, 120m) },
                new Dictionary<ExpenseCategory, decimal>()),
        });

    [Fact]
    public async Task The_budgets_are_sent_to_the_budget_insights_endpoint_with_the_contract_values()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);

        await Recommend(handler);

        handler.Path.ShouldBe("/api/v1/budget-insights/recommendations");
        using var body = JsonDocument.Parse(handler.Bodies.Single());
        var root = body.RootElement;
        root.GetProperty("language").GetString().ShouldBe("en");
        root.GetProperty("currency").GetString().ShouldBe("USD");
        var september = root.GetProperty("months")[0];
        september.GetProperty("month").GetString().ShouldBe("2026-09");
        september.GetProperty("is_current").GetBoolean().ShouldBeFalse();
        september.GetProperty("limits")[0].GetProperty("category").GetString().ShouldBe("alimentos");
        september.GetProperty("limits")[0].GetProperty("limit").GetDecimal().ShouldBe(300m);
        september.GetProperty("limits")[0].GetProperty("spent").GetDecimal().ShouldBe(350m);
        var unbudgeted = september.GetProperty("unbudgeted_expenses");
        unbudgeted.GetArrayLength().ShouldBe(1);
        unbudgeted[0].GetProperty("category").GetString().ShouldBe("entretenimiento");
        root.GetProperty("months")[1].GetProperty("is_current").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task The_recommendations_come_back_with_their_kind_and_action()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);

        var result = await Recommend(handler);

        result.Recommendations.Select(recommendation => (recommendation.Kind, recommendation.Action)).ShouldBe(new[]
        {
            (RecommendationKind.Positive, RecommendationAction.Budgets),
            (RecommendationKind.Warning, RecommendationAction.Transactions),
            (RecommendationKind.Tip, RecommendationAction.Budgets),
        });
    }

    [Fact]
    public async Task An_error_status_from_FinGrow_AI_is_an_http_failure()
    {
        using var handler = new RecordingHandler("""{"code": "inference_failed", "message": "x"}""", HttpStatusCode.BadGateway);

        await Should.ThrowAsync<HttpRequestException>(() => Recommend(handler));
    }

    [Fact]
    public async Task The_same_budgets_are_answered_from_the_cache()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new AiServiceOptions { RecommendationsCacheHours = 6 });

        await Recommend(handler, cache, options);
        await Recommend(handler, cache, options);

        handler.Bodies.Count.ShouldBe(1);
        cache.Count.ShouldBe(1);
    }

    private static async Task<FinancialRecommendations> Recommend(
        RecordingHandler handler,
        IMemoryCache? cache = null,
        IOptions<AiServiceOptions>? options = null)
    {
        using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://fingrow-ai.test") };

        return await new AiService(httpClient, cache, options).RecommendBudgetAsync(Input);
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

        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;

            if (request.Content is not null)
            {
                Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            };
        }
    }
}

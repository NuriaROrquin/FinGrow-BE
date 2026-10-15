namespace FinGrow.Api.UnitTests.Ai;

using System.Net;
using System.Text;
using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Infrastructure.Ai;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public class AiServiceRecommendationsTests
{
    private const string ThreeRecommendations = """
        {
          "recommendations": [
            {"kind": "positive", "title": "Vas bien", "body": "Ahorraste el 20%.", "action": "goals"},
            {"kind": "warning", "title": "Alimentos", "body": "Te pasaste del tope.", "action": "budgets"},
            {"kind": "tip", "title": "Educación", "body": "Aprendé sobre ahorro.", "action": "education"}
          ],
          "model": "m"
        }
        """;

    private static readonly RecommendationsInput Input = new(
        Language.pt,
        Currency.ARS,
        new DateOnly(2026, 9, 1),
        new DateOnly(2026, 10, 15),
        new[]
        {
            new RecommendationMonth(2026, 8, true, 1000m, 700m, 300m, new Dictionary<ExpenseCategory, decimal> { [ExpenseCategory.Alimentos] = 700m }),
            new RecommendationMonth(2026, 9, false, 1200m, 900m, 400m, new Dictionary<ExpenseCategory, decimal>
            {
                [ExpenseCategory.Alimentos] = 800m,
                [ExpenseCategory.AhorroInversion] = 100m,
            }),
        },
        new[] { new RecommendationBudgetLimit(ExpenseCategory.Alimentos, 750m, 800m) },
        new[] { new RecommendationGoal("Vacaciones", false, 12000m, 3000m, 1000m, new DateOnly(2027, 6, 30)) });

    [Fact]
    public async Task The_summary_is_sent_to_FinGrow_AI_with_the_contract_values()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);

        await Recommend(handler);

        handler.Path.ShouldBe("/api/v1/financial-insights/recommendations");
        using var body = JsonDocument.Parse(handler.Bodies.Single());
        var root = body.RootElement;
        root.GetProperty("language").GetString().ShouldBe("pt");
        root.GetProperty("currency").GetString().ShouldBe("ARS");
        root.GetProperty("period_start").GetString().ShouldBe("2026-09-01");
        root.GetProperty("period_end").GetString().ShouldBe("2026-10-15");
        var september = root.GetProperty("months")[1];
        september.GetProperty("month").GetString().ShouldBe("2026-09");
        september.GetProperty("is_reference").GetBoolean().ShouldBeFalse();
        september.GetProperty("expenses_by_category")[0].GetProperty("category").GetString().ShouldBe("alimentos");
        september.GetProperty("expenses_by_category")[1].GetProperty("category").GetString().ShouldBe("ahorro_inversion");
        root.GetProperty("budget_limits")[0].GetProperty("spent").GetDecimal().ShouldBe(800m);
        var goal = root.GetProperty("goals")[0];
        goal.GetProperty("name").GetString().ShouldBe("Vacaciones");
        goal.GetProperty("status").GetString().ShouldBe("active");
        goal.GetProperty("deadline").GetString().ShouldBe("2027-06-30");
    }

    [Fact]
    public async Task The_recommendations_come_back_with_their_kind_and_action()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);

        var result = await Recommend(handler);

        result.Model.ShouldBe("m");
        result.Recommendations.Select(recommendation => (recommendation.Kind, recommendation.Action)).ShouldBe(new[]
        {
            (RecommendationKind.Positive, RecommendationAction.Goals),
            (RecommendationKind.Warning, RecommendationAction.Budgets),
            (RecommendationKind.Tip, RecommendationAction.Education),
        });
        result.Recommendations[0].Title.ShouldBe("Vas bien");
    }

    [Fact]
    public async Task A_recommendation_with_an_unknown_kind_or_action_is_dropped()
    {
        using var handler = new RecordingHandler("""
            {
              "recommendations": [
                {"kind": "inventado", "title": "A", "body": "B", "action": "goals"},
                {"kind": "tip", "title": "A", "body": "B", "action": "home"},
                {"kind": "tip", "title": " ", "body": "B", "action": "none"},
                {"kind": "tip", "title": "Registrá todo", "body": "Cargá tus gastos.", "action": "transactions"}
              ],
              "model": "m"
            }
            """);

        var result = await Recommend(handler);

        result.Recommendations.ShouldHaveSingleItem().Action.ShouldBe(RecommendationAction.Transactions);
    }

    [Theory]
    [InlineData("""{"recommendations": [], "model": "m"}""")]
    [InlineData("""{"recommendations": [{"kind": "tip"}], "model": "m"}""")]
    [InlineData("no es json")]
    public async Task An_answer_without_valid_recommendations_is_an_http_failure(string response)
    {
        using var handler = new RecordingHandler(response);

        await Should.ThrowAsync<HttpRequestException>(() => Recommend(handler));
    }

    [Fact]
    public async Task An_error_status_from_FinGrow_AI_is_an_http_failure()
    {
        using var handler = new RecordingHandler("""{"code": "inference_failed", "message": "x"}""", HttpStatusCode.BadGateway);

        await Should.ThrowAsync<HttpRequestException>(() => Recommend(handler));
    }

    [Fact]
    public async Task The_same_summary_is_answered_from_the_cache()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new AiServiceOptions { RecommendationsCacheHours = 6 });

        await Recommend(handler, cache, options);
        var second = await Recommend(handler, cache, options);
        await Recommend(handler, cache, options, Input with { Currency = Currency.USD });

        second.Recommendations.Count.ShouldBe(3);
        handler.Bodies.Count.ShouldBe(2);
    }

    [Fact]
    public async Task With_zero_hours_nothing_is_cached()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new AiServiceOptions { RecommendationsCacheHours = 0 });

        await Recommend(handler, cache, options);
        await Recommend(handler, cache, options);

        handler.Bodies.Count.ShouldBe(2);
    }

    private static async Task<FinancialRecommendations> Recommend(
        RecordingHandler handler,
        IMemoryCache? cache = null,
        IOptions<AiServiceOptions>? options = null,
        RecommendationsInput? input = null)
    {
        using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://fingrow-ai.test") };

        return await new AiService(httpClient, cache, options).RecommendAsync(input ?? Input);
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

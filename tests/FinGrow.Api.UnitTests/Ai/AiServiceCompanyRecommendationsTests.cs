namespace FinGrow.Api.UnitTests.Ai;

using System.Net;
using System.Text;
using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Infrastructure.Ai;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public class AiServiceCompanyRecommendationsTests
{
    private const string ThreeRecommendations = """
        {
          "recommendations": [
            {"kind": "warning", "title": "Fondo de emergencia", "body": "Un taller puede ayudar.", "action": "none"},
            {"kind": "tip", "title": "Presupuestos", "body": "Una charla sobre topes.", "action": "none"},
            {"kind": "positive", "title": "Participación", "body": "Sigue creciendo.", "action": "none"}
          ],
          "model": "m"
        }
        """;

    private static readonly string[] ConcernWireValues =
    {
        "thin_cushion",
        "low_savings",
        "budget_overruns",
        "missed_goal_contributions",
    };

    private static readonly string[] RequestFields =
    {
        "language",
        "month",
        "measured_employees",
        "average_score",
        "participation_rate",
        "concerns",
    };

    private static readonly string[] ConcernFields =
    {
        "concern",
        "affected_employees",
        "evaluated_employees",
        "percentage",
    };

    private static readonly CompanyRecommendationsInput Input = new(
        Language.pt,
        2026,
        9,
        12,
        61.5m,
        null,
        new[]
        {
            new CompanyConcernSummary(CompanyConcern.ThinCushion, 5, 12, 41.67m),
            new CompanyConcernSummary(CompanyConcern.LowSavings, 4, 12, 33.33m),
            new CompanyConcernSummary(CompanyConcern.BudgetOverruns, 3, 8, 37.5m),
            new CompanyConcernSummary(CompanyConcern.MissedGoalContributions, 3, 3, 100m),
        });

    [Fact]
    public async Task The_aggregates_are_sent_to_the_company_insights_endpoint_with_the_contract_values()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);

        await Recommend(handler);

        handler.Path.ShouldBe("/api/v1/company-insights/recommendations");
        using var body = JsonDocument.Parse(handler.Bodies.Single());
        var root = body.RootElement;
        root.GetProperty("language").GetString().ShouldBe("pt");
        root.GetProperty("month").GetString().ShouldBe("2026-09");
        root.GetProperty("measured_employees").GetInt32().ShouldBe(12);
        root.GetProperty("average_score").GetDecimal().ShouldBe(61.5m);
        root.GetProperty("participation_rate").ValueKind.ShouldBe(JsonValueKind.Null);
        var concerns = root.GetProperty("concerns");
        concerns.EnumerateArray().Select(concern => concern.GetProperty("concern").GetString()).ShouldBe(ConcernWireValues);
        concerns[0].GetProperty("affected_employees").GetInt32().ShouldBe(5);
        concerns[0].GetProperty("evaluated_employees").GetInt32().ShouldBe(12);
        concerns[0].GetProperty("percentage").GetDecimal().ShouldBe(41.67m);
    }

    [Fact]
    public async Task Only_aggregated_fields_travel_to_FinGrow_AI()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);

        await Recommend(handler);

        using var body = JsonDocument.Parse(handler.Bodies.Single());
        body.RootElement.EnumerateObject().Select(property => property.Name)
            .ShouldBe(RequestFields, ignoreOrder: true);
        body.RootElement.GetProperty("concerns")[0].EnumerateObject().Select(property => property.Name)
            .ShouldBe(ConcernFields, ignoreOrder: true);
    }

    [Fact]
    public async Task The_recommendations_come_back_without_a_screen()
    {
        using var handler = new RecordingHandler(ThreeRecommendations);

        var result = await Recommend(handler);

        result.Recommendations.Select(recommendation => (recommendation.Kind, recommendation.Action)).ShouldBe(new[]
        {
            (RecommendationKind.Warning, RecommendationAction.None),
            (RecommendationKind.Tip, RecommendationAction.None),
            (RecommendationKind.Positive, RecommendationAction.None),
        });
    }

    [Fact]
    public async Task An_error_status_from_FinGrow_AI_is_an_http_failure()
    {
        using var handler = new RecordingHandler("""{"code": "model_unavailable", "message": "x"}""", HttpStatusCode.ServiceUnavailable);

        await Should.ThrowAsync<HttpRequestException>(() => Recommend(handler));
    }

    [Fact]
    public async Task The_same_aggregates_are_answered_from_the_cache()
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

        return await new AiService(httpClient, cache, options).RecommendCompanyAsync(Input);
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

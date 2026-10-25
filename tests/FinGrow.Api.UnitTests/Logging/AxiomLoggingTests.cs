namespace FinGrow.Api.UnitTests.Logging;

using System.Text;
using FinGrow.Api.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

public class AxiomLoggingTests
{
    [Fact]
    public async Task Logs_reach_the_otlp_logs_endpoint_with_the_token_and_the_dataset()
    {
        await using var collector = await FakeOtlpCollector.StartAsync();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Axiom:ApiToken"] = "xaat-unit-test-token",
            ["Axiom:Dataset"] = "fingrow-test",
            ["Axiom:Domain"] = collector.BaseUrl,
        });

        await using (var logger = new LoggerConfiguration()
            .WriteToAxiom(configuration, new FakeHostEnvironment("Testing"))
            .CreateLogger())
        {
            logger.Information("Corrida de {Job} terminada", "metrics-snapshot");
        }

        var request = await collector.FirstRequest.WaitAsync(TimeSpan.FromSeconds(10));
        request.Path.ShouldBe("/v1/logs");
        request.Authorization.ShouldBe("Bearer xaat-unit-test-token");
        request.Dataset.ShouldBe("fingrow-test");
        request.Body.ShouldContain(AxiomOptions.ServiceName);
        request.Body.ShouldContain("Testing");
        request.Body.ShouldContain("metrics-snapshot");
    }

    [Fact]
    public async Task Nothing_is_sent_when_axiom_is_not_configured()
    {
        await using var collector = await FakeOtlpCollector.StartAsync();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        await using (var logger = new LoggerConfiguration()
            .WriteToAxiom(configuration, new FakeHostEnvironment("Testing"))
            .CreateLogger())
        {
            logger.Information("Arranque de la API");
        }

        collector.FirstRequest.IsCompleted.ShouldBeFalse();
    }

    [Theory]
    [InlineData("xaat-unit-test-token", "", "Axiom:Dataset")]
    [InlineData("", "fingrow-test", "Axiom:ApiToken")]
    public void A_half_configured_axiom_stops_the_startup_naming_what_is_missing(
        string apiToken, string dataset, string missingSetting)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Axiom:ApiToken"] = apiToken,
            ["Axiom:Dataset"] = dataset,
        });

        var exception = Should.Throw<InvalidOperationException>(() =>
            new LoggerConfiguration().WriteToAxiom(configuration, new FakeHostEnvironment("Testing")));

        exception.Message.ShouldContain(missingSetting);
    }

    [Theory]
    [InlineData("", "https://api.axiom.co/v1/logs")]
    [InlineData("us-east-1.aws.edge.axiom.co", "https://us-east-1.aws.edge.axiom.co/v1/logs")]
    [InlineData("eu-central-1.aws.edge.axiom.co/", "https://eu-central-1.aws.edge.axiom.co/v1/logs")]
    [InlineData("http://127.0.0.1:4318", "http://127.0.0.1:4318/v1/logs")]
    public void The_logs_endpoint_is_built_from_the_domain(string domain, string expectedEndpoint)
    {
        var options = new AxiomOptions { Domain = domain };

        options.LogsEndpoint.ShouldBe(expectedEndpoint);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private sealed record CapturedRequest(string Path, string? Authorization, string? Dataset, string Body);

    private sealed class FakeOtlpCollector : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly TaskCompletionSource<CapturedRequest> _firstRequest =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private FakeOtlpCollector()
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            _app = builder.Build();
            _app.MapPost("/{**path}", async (HttpContext context) =>
            {
                using var buffer = new MemoryStream();
                await context.Request.Body.CopyToAsync(buffer);
                _firstRequest.TrySetResult(new CapturedRequest(
                    context.Request.Path.Value ?? string.Empty,
                    context.Request.Headers.Authorization.ToString(),
                    context.Request.Headers[AxiomOptions.DatasetHeader].ToString(),
                    Encoding.UTF8.GetString(buffer.ToArray())));
                return Results.Ok();
            });
        }

        public Task<CapturedRequest> FirstRequest => _firstRequest.Task;

        public string BaseUrl => _app.Urls.First();

        public static async Task<FakeOtlpCollector> StartAsync()
        {
            var collector = new FakeOtlpCollector();
            await collector._app.StartAsync();
            return collector;
        }

        public async ValueTask DisposeAsync() => await _app.DisposeAsync();
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "FinGrow.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

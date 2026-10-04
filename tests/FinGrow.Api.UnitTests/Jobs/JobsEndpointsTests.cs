namespace FinGrow.Api.UnitTests.Jobs;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinGrow.Api.Jobs;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class JobsEndpointsTests
{
    private const string ApiKey = "unit-test-jobs-api-key-1234";

    [Fact]
    public async Task Without_the_key_the_endpoints_are_forbidden()
    {
        using var factory = new JobsWebApplicationFactory();
        var client = factory.CreateClient();

        var list = await client.GetAsync(new Uri("/api/jobs", UriKind.Relative));
        var run = await client.PostAsync(new Uri("/api/jobs/fake-job/run", UriKind.Relative), content: null);

        list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        run.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        factory.Job.Executions.ShouldBe(0);
        factory.Runs.Runs.ShouldBeEmpty();
    }

    [Fact]
    public async Task With_a_wrong_key_the_run_is_forbidden_and_nothing_executes()
    {
        using var factory = new JobsWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ValidateJobsKeyAttribute.KeyHeader, "otra-clave-cualquiera-123");

        var response = await client.PostAsync(new Uri("/api/jobs/fake-job/run", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        factory.Job.Executions.ShouldBe(0);
    }

    [Fact]
    public async Task With_the_key_the_job_runs_and_the_recorded_run_comes_back()
    {
        using var factory = new JobsWebApplicationFactory();
        var client = Authorized(factory);

        var response = await client.PostAsync(new Uri("/api/jobs/fake-job/run", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("jobName").GetString().ShouldBe("fake-job");
        body.GetProperty("status").GetString().ShouldBe("Succeeded");
        body.GetProperty("summary").GetString().ShouldBe("todo en orden");
        body.GetProperty("finishedAt").ValueKind.ShouldBe(JsonValueKind.String);
        factory.Job.Executions.ShouldBe(1);
        factory.Runs.Runs.ShouldHaveSingleItem().JobName.ShouldBe("fake-job");
    }

    [Fact]
    public async Task A_failed_job_is_still_reported_with_its_error()
    {
        using var factory = new JobsWebApplicationFactory();
        factory.Job.Result = JobResult.Failure("se rompio", "parcial");
        var client = Authorized(factory);

        var response = await client.PostAsync(new Uri("/api/jobs/fake-job/run", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().ShouldBe("Failed");
        body.GetProperty("error").GetString().ShouldBe("se rompio");
        body.GetProperty("summary").GetString().ShouldBe("parcial");
    }

    [Fact]
    public async Task An_unknown_job_returns_404()
    {
        using var factory = new JobsWebApplicationFactory();
        var client = Authorized(factory);

        var run = await client.PostAsync(new Uri("/api/jobs/no-existe/run", UriKind.Relative), content: null);
        var runs = await client.GetAsync(new Uri("/api/jobs/no-existe/runs", UriKind.Relative));

        run.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        runs.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_job_list_shows_each_job_with_its_last_run()
    {
        using var factory = new JobsWebApplicationFactory();
        var client = Authorized(factory);
        await client.PostAsync(new Uri("/api/jobs/fake-job/run", UriKind.Relative), content: null);

        var response = await client.GetAsync(new Uri("/api/jobs", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var jobs = await response.Content.ReadFromJsonAsync<JsonElement>();
        var job = jobs.EnumerateArray().ShouldHaveSingleItem();
        job.GetProperty("name").GetString().ShouldBe("fake-job");
        job.GetProperty("schedule").GetString().ShouldBe("*/5 * * * *");
        job.GetProperty("lastRun").GetProperty("status").GetString().ShouldBe("Succeeded");
    }

    [Fact]
    public async Task Recent_runs_can_be_listed_and_the_page_size_is_validated()
    {
        using var factory = new JobsWebApplicationFactory();
        var client = Authorized(factory);
        await client.PostAsync(new Uri("/api/jobs/fake-job/run", UriKind.Relative), content: null);
        await client.PostAsync(new Uri("/api/jobs/fake-job/run", UriKind.Relative), content: null);

        var runs = await client.GetAsync(new Uri("/api/jobs/fake-job/runs?take=1", UriKind.Relative));
        var invalid = await client.GetAsync(new Uri("/api/jobs/fake-job/runs?take=0", UriKind.Relative));

        runs.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await runs.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Count().ShouldBe(1);
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Runs.Runs.Count.ShouldBe(2);
    }

    private static HttpClient Authorized(JobsWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ValidateJobsKeyAttribute.KeyHeader, ApiKey);

        return client;
    }

    private sealed class JobsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public RecordingJob Job { get; } = new();

        public InMemoryJobRunRepository Runs { get; } = new();

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
                    ["Jobs:ApiKey"] = ApiKey,
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IScheduledJob>();
                services.AddScoped<IScheduledJob>(_ => Job);
                services.AddScoped<IJobRunRepository>(_ => Runs);
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    private sealed class RecordingJob : IScheduledJob
    {
        public string Name => "fake-job";

        public string Description => "Trabajo de prueba";

        public string Schedule => "*/5 * * * *";

        public int Executions { get; private set; }

        public JobResult Result { get; set; } = JobResult.Success("todo en orden");

        public Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            Executions++;

            return Task.FromResult(Result);
        }
    }

    private sealed class InMemoryJobRunRepository : IJobRunRepository
    {
        public List<JobRun> Runs { get; } = new();

        public void Add(JobRun run) => Runs.Add(run);

        public Task<JobRun?> FindLastAsync(string jobName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Runs.Where(run => run.JobName == jobName).OrderByDescending(run => run.StartedAt).FirstOrDefault());

        public Task<IReadOnlyList<JobRun>> ListRecentAsync(string jobName, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<JobRun>>(Runs.Where(run => run.JobName == jobName).OrderByDescending(run => run.StartedAt).Take(take).ToList());
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}

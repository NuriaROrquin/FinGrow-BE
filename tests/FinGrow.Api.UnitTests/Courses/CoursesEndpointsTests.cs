namespace FinGrow.Api.UnitTests.Courses;

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

public class CoursesEndpointsTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Every_query_parameter_reaches_the_filters()
    {
        using var factory = new CoursesWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri(
            "/api/courses?level=Intermediate&maxDuration=45&status=NotStarted",
            UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Courses.WasQueried.ShouldBeTrue();
        factory.Courses.RequestedLevel.ShouldBe(CourseLevel.Intermediate);
        factory.Courses.RequestedMaxDurationMinutes.ShouldBe(45);
        factory.Courses.RequestedEmployeeId.ShouldBe(EmployeeId);
    }

    [Fact]
    public async Task Each_course_comes_with_its_level_duration_and_the_progress_of_the_employee()
    {
        using var factory = new CoursesWebApplicationFactory();
        var course = PublishedCourse();
        factory.Courses.Courses.Add(course);
        factory.Courses.CompletedLessonIds.Add(course.Lessons.First().Id);
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/courses", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().ShouldBe(1);
        var item = body[0];
        item.GetProperty("slug").GetString().ShouldBe("fundamentos-finanzas-personales");
        item.GetProperty("level").GetString().ShouldBe("Beginner");
        item.GetProperty("category").GetString().ShouldBe("Basics");
        item.GetProperty("relatedInvestmentType").GetString().ShouldBe("Etf");
        item.GetProperty("durationMinutes").GetInt32().ShouldBe(25);
        item.GetProperty("lessonCount").GetInt32().ShouldBe(2);
        item.GetProperty("completedLessons").GetInt32().ShouldBe(1);
        item.GetProperty("progressPercentage").GetInt32().ShouldBe(50);
        item.GetProperty("progressStatus").GetString().ShouldBe("InProgress");
    }

    [Theory]
    [InlineData("maxDuration=0")]
    [InlineData("maxDuration=-5")]
    [InlineData("level=Experto")]
    [InlineData("status=Terminado")]
    public async Task An_invalid_filter_is_rejected_with_400_without_querying_the_database(string query)
    {
        using var factory = new CoursesWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri($"/api/courses?{query}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Courses.WasQueried.ShouldBeFalse();
    }

    [Fact]
    public async Task Without_a_session_the_catalog_is_not_served()
    {
        using var factory = new CoursesWebApplicationFactory();

        var response = await factory.CreateClient().GetAsync(new Uri("/api/courses", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Courses.WasQueried.ShouldBeFalse();
    }

    [Fact]
    public async Task A_company_session_cannot_list_the_courses()
    {
        using var factory = new CoursesWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empresa);

        var response = await client.GetAsync(new Uri("/api/courses", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        factory.Courses.WasQueried.ShouldBeFalse();
    }

    [Fact]
    public async Task Opening_a_course_returns_its_lessons_and_the_lesson_where_to_resume()
    {
        using var factory = new CoursesWebApplicationFactory();
        var course = PublishedCourse();
        var lessons = course.Lessons.ToList();
        factory.Courses.Courses.Add(course);
        factory.Courses.CompletedLessonIds.Add(lessons[0].Id);
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/courses/fundamentos-finanzas-personales", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Courses.RequestedEmployeeId.ShouldBe(EmployeeId);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("progressStatus").GetString().ShouldBe("InProgress");
        body.GetProperty("resumeLessonId").GetGuid().ShouldBe(lessons[1].Id);
        var items = body.GetProperty("lessons");
        items.GetArrayLength().ShouldBe(2);
        items[0].GetProperty("position").GetInt32().ShouldBe(1);
        items[0].GetProperty("isCompleted").GetBoolean().ShouldBeTrue();
        items[1].GetProperty("title").GetString().ShouldBe("Presupuesto");
        items[1].GetProperty("durationMinutes").GetInt32().ShouldBe(15);
        items[1].GetProperty("videoUrl").GetString().ShouldBe("https://www.youtube.com/embed/def");
        items[1].GetProperty("isCompleted").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task A_course_that_does_not_exist_is_404()
    {
        using var factory = new CoursesWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/courses/no-existe", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().ShouldBe("Course.NotFound");
    }

    [Fact]
    public async Task Completing_a_lesson_returns_the_updated_progress()
    {
        using var factory = new CoursesWebApplicationFactory();
        var course = PublishedCourse();
        var lessons = course.Lessons.ToList();
        factory.Courses.Courses.Add(course);
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.PutAsync(
            new Uri($"/api/courses/fundamentos-finanzas-personales/lessons/{lessons[0].Id}/completion", UriKind.Relative),
            null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Courses.CompletedLessonIds.ShouldContain(lessons[0].Id);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("completedLessons").GetInt32().ShouldBe(1);
        body.GetProperty("progressPercentage").GetInt32().ShouldBe(50);
        body.GetProperty("resumeLessonId").GetGuid().ShouldBe(lessons[1].Id);
    }

    [Fact]
    public async Task Completing_a_lesson_that_is_not_in_the_course_is_404()
    {
        using var factory = new CoursesWebApplicationFactory();
        factory.Courses.Courses.Add(PublishedCourse());
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.PutAsync(
            new Uri($"/api/courses/fundamentos-finanzas-personales/lessons/{Guid.CreateVersion7()}/completion", UriKind.Relative),
            null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        factory.Courses.CompletedLessonIds.ShouldBeEmpty();
    }

    private static Course PublishedCourse()
    {
        var course = Course.Create(
            "fundamentos-finanzas-personales",
            "Fundamentos de Finanzas Personales",
            "Aprendé a gestionar tu dinero.",
            CourseLevel.Beginner,
            EducationCategory.Basics,
            InvestmentType.Etf,
            Now);
        course.AddLesson("Introducción", 10, "https://www.youtube.com/embed/abc", Now);
        course.AddLesson("Presupuesto", 15, "https://www.youtube.com/embed/def", Now);
        course.Publish(Now);

        return course;
    }

    private static HttpClient AuthenticatedClient(CoursesWebApplicationFactory factory, string role)
    {
        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(EmployeeId, Guid.CreateVersion7(), role, "Ana Gomez");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class CapturingCourseRepository : ICourseRepository
    {
        public List<Course> Courses { get; } = new();

        public HashSet<Guid> CompletedLessonIds { get; } = new();

        public bool WasQueried { get; private set; }

        public CourseLevel? RequestedLevel { get; private set; }

        public int? RequestedMaxDurationMinutes { get; private set; }

        public Guid? RequestedEmployeeId { get; private set; }

        public Task<IReadOnlyList<Course>> ListPublishedAsync(
            CourseLevel? level,
            int? maxDurationMinutes,
            CancellationToken cancellationToken = default)
        {
            WasQueried = true;
            RequestedLevel = level;
            RequestedMaxDurationMinutes = maxDurationMinutes;

            return Task.FromResult<IReadOnlyList<Course>>(Courses.ToList());
        }

        public Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(Courses.FirstOrDefault(course => course.Slug == slug));

        public Task<IReadOnlySet<Guid>> ListCompletedLessonIdsAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            RequestedEmployeeId = employeeId;

            return Task.FromResult<IReadOnlySet<Guid>>(CompletedLessonIds.ToHashSet());
        }

        public void AddCompletion(LessonCompletion completion) => CompletedLessonIds.Add(completion.LessonId);
    }

    private sealed class CoursesWebApplicationFactory : WebApplicationFactory<Program>
    {
        public CapturingCourseRepository Courses { get; } = new();

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
                services.RemoveAll<ICourseRepository>();
                services.AddSingleton<ICourseRepository>(Courses);
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}

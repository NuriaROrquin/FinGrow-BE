namespace FinGrow.Application.UnitTests.Features.Courses.GetCourse;

using Common;
using DTOs;
using FinGrow.Application.Features.Courses.GetCourse;
using Fakes;
using Domain.Entities;
using Domain.Enums;

public class GetCourseHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly int[] ExpectedPositions = { 1, 2, 3 };

    private readonly FakeCourseRepository _courses = new();

    private Course StoreCourse(string slug = "fundamentos-finanzas-personales", bool published = true)
    {
        var course = Course.Create(
            slug,
            "Fundamentos de Finanzas Personales",
            "Una descripcion corta.",
            CourseLevel.Beginner,
            EducationCategory.Basics,
            relatedInvestmentType: null,
            Now);

        course.AddLesson("Introduccion", 10, "https://www.youtube.com/embed/a", Now);
        course.AddLesson("Presupuesto", 15, "https://www.youtube.com/embed/b", Now);
        course.AddLesson("Ahorro", 12, "https://www.youtube.com/embed/c", Now);

        if (published)
        {
            course.Publish(Now);
        }

        _courses.Add(course);
        return course;
    }

    private Task<Result<CourseDetailResponse>> GetAsync(string slug = "fundamentos-finanzas-personales") =>
        new GetCourseHandler(_courses).Handle(new GetCourseQuery(EmployeeId, slug), CancellationToken.None);

    [Fact]
    public async Task Opening_a_course_returns_its_lessons_in_order_with_their_videos()
    {
        StoreCourse();

        var course = (await GetAsync()).Value;

        course.Slug.ShouldBe("fundamentos-finanzas-personales");
        course.DurationMinutes.ShouldBe(37);
        course.Lessons.Select(lesson => lesson.Position).ShouldBe(ExpectedPositions);
        course.Lessons[1].Title.ShouldBe("Presupuesto");
        course.Lessons[1].VideoUrl.ShouldBe("https://www.youtube.com/embed/b");
    }

    [Fact]
    public async Task A_course_not_started_resumes_at_the_first_lesson()
    {
        var stored = StoreCourse();

        var course = (await GetAsync()).Value;

        course.ProgressStatus.ShouldBe(CourseProgressStatus.NotStarted);
        course.ResumeLessonId.ShouldBe(stored.Lessons.First().Id);
        course.Lessons.ShouldAllBe(lesson => !lesson.IsCompleted);
    }

    [Fact]
    public async Task A_started_course_resumes_at_the_lesson_where_the_employee_left_off()
    {
        var stored = StoreCourse();
        var lessons = stored.Lessons.ToList();
        _courses.Complete(EmployeeId, lessons[0]);

        var course = (await GetAsync()).Value;

        course.ResumeLessonId.ShouldBe(lessons[1].Id);
        course.CompletedLessons.ShouldBe(1);
        course.ProgressPercentage.ShouldBe(33);
        course.ProgressStatus.ShouldBe(CourseProgressStatus.InProgress);
        course.Lessons[0].IsCompleted.ShouldBeTrue();
        course.Lessons[1].IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task The_progress_of_another_employee_does_not_count()
    {
        var stored = StoreCourse();
        _courses.Complete(Guid.CreateVersion7(), stored.Lessons.First());

        var course = (await GetAsync()).Value;

        course.CompletedLessons.ShouldBe(0);
        course.ResumeLessonId.ShouldBe(stored.Lessons.First().Id);
    }

    [Theory]
    [InlineData("no-existe")]
    [InlineData("borrador")]
    public async Task A_missing_or_draft_course_is_not_found(string slug)
    {
        StoreCourse("borrador", published: false);

        var result = await GetAsync(slug);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}

namespace FinGrow.Application.UnitTests.Features.Courses.GetLearningProgress;

using DTOs;
using FinGrow.Application.Features.Courses.GetLearningProgress;
using Fakes;
using Domain.Entities;
using Domain.Enums;

public class GetLearningProgressHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeCourseRepository _courses = new();

    private Course StoreCourse(string slug, int lessonCount, bool published = true)
    {
        var course = Course.Create(
            slug,
            $"Titulo {slug}",
            "Una descripcion corta.",
            CourseLevel.Beginner,
            EducationCategory.Basics,
            relatedInvestmentType: null,
            Now);

        for (var position = 1; position <= lessonCount; position++)
        {
            course.AddLesson($"Leccion {position}", 10, "https://www.youtube.com/embed/abc", Now);
        }

        if (published)
        {
            course.Publish(Now);
        }

        _courses.Add(course);
        return course;
    }

    private void Complete(Guid employeeId, Course course, int lessons)
    {
        foreach (var lesson in course.Lessons.Take(lessons))
        {
            _courses.Complete(employeeId, lesson);
        }
    }

    private async Task<LearningProgressResponse> GetAsync() =>
        (await new GetLearningProgressHandler(_courses).Handle(
            new GetLearningProgressQuery(EmployeeId),
            CancellationToken.None)).Value;

    [Fact]
    public async Task The_progress_counts_completed_and_in_progress_courses_and_the_overall_percentage()
    {
        Complete(EmployeeId, StoreCourse("terminado", 2), 2);
        Complete(EmployeeId, StoreCourse("a-medias", 4), 1);
        StoreCourse("sin-empezar", 4);

        var progress = await GetAsync();

        progress.CourseCount.ShouldBe(3);
        progress.CompletedCourses.ShouldBe(1);
        progress.InProgressCourses.ShouldBe(1);
        progress.NotStartedCourses.ShouldBe(1);
        progress.CompletedLessons.ShouldBe(3);
        progress.LessonCount.ShouldBe(10);
        progress.ProgressPercentage.ShouldBe(30);
    }

    [Fact]
    public async Task An_employee_without_activity_has_everything_at_zero()
    {
        StoreCourse("nuevo", 3);

        var progress = await GetAsync();

        progress.CourseCount.ShouldBe(1);
        progress.CompletedCourses.ShouldBe(0);
        progress.InProgressCourses.ShouldBe(0);
        progress.NotStartedCourses.ShouldBe(1);
        progress.ProgressPercentage.ShouldBe(0);
    }

    [Fact]
    public async Task Another_employee_progress_does_not_count()
    {
        Complete(Guid.CreateVersion7(), StoreCourse("ajeno", 2), 2);

        var progress = await GetAsync();

        progress.CompletedCourses.ShouldBe(0);
        progress.CompletedLessons.ShouldBe(0);
    }

    [Fact]
    public async Task Drafts_do_not_count_towards_the_progress()
    {
        Complete(EmployeeId, StoreCourse("publicado", 2), 1);
        StoreCourse("borrador", 8, published: false);

        var progress = await GetAsync();

        progress.CourseCount.ShouldBe(1);
        progress.LessonCount.ShouldBe(2);
        progress.ProgressPercentage.ShouldBe(50);
    }

    [Fact]
    public async Task Without_published_courses_the_progress_is_empty()
    {
        var progress = await GetAsync();

        progress.CourseCount.ShouldBe(0);
        progress.ProgressPercentage.ShouldBe(0);
    }
}

namespace FinGrow.Application.UnitTests.Features.Courses.ListCourses;

using Common;
using DTOs;
using FinGrow.Application.Features.Courses.ListCourses;
using Fakes;
using Domain.Entities;
using Domain.Enums;

public class ListCoursesHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private static readonly int[] DefaultLessonMinutes = { 10, 10 };
    private static readonly string[] SlugsByLevel = { "basico", "intermedio", "avanzado" };

    private readonly FakeCourseRepository _courses = new();
    private readonly FakeCourseRatingRepository _ratings = new();

    private Course StoreCourse(string slug, CourseLevel level = CourseLevel.Beginner, params int[] lessonMinutes) =>
        StoreCourse(slug, level, published: true, lessonMinutes);

    private Course StoreCourse(string slug, CourseLevel level, bool published, params int[] lessonMinutes)
    {
        var course = Course.Create(
            slug,
            $"Titulo {slug}",
            "Una descripcion corta.",
            level,
            EducationCategory.Basics,
            relatedInvestmentType: null,
            Now);

        var position = 0;
        foreach (var minutes in lessonMinutes.Length == 0 ? DefaultLessonMinutes : lessonMinutes)
        {
            course.AddLesson($"Leccion {++position}", minutes, "https://www.youtube.com/embed/abc", Now);
        }

        if (published)
        {
            course.Publish(Now);
        }

        _courses.Add(course);
        return course;
    }

    private Task<Result<IReadOnlyList<CourseSummaryResponse>>> ListAsync(
        CourseLevel? level = null,
        int? maxDuration = null,
        CourseProgressStatus? status = null) =>
        new ListCoursesHandler(_courses, _ratings).Handle(
            new ListCoursesQuery(EmployeeId, level, maxDuration, status),
            CancellationToken.None);

    [Fact]
    public async Task Each_course_shows_its_level_duration_and_lesson_count()
    {
        StoreCourse("finanzas-personales", CourseLevel.Intermediate, 10, 15, 12);

        var course = (await ListAsync()).Value.ShouldHaveSingleItem();

        course.Slug.ShouldBe("finanzas-personales");
        course.Level.ShouldBe(CourseLevel.Intermediate);
        course.DurationMinutes.ShouldBe(37);
        course.LessonCount.ShouldBe(3);
    }

    [Fact]
    public async Task A_course_without_completed_lessons_is_not_started()
    {
        StoreCourse("sin-empezar");

        var course = (await ListAsync()).Value.ShouldHaveSingleItem();

        course.CompletedLessons.ShouldBe(0);
        course.ProgressPercentage.ShouldBe(0);
        course.ProgressStatus.ShouldBe(CourseProgressStatus.NotStarted);
    }

    [Fact]
    public async Task Progress_counts_the_completed_lessons_rounding_down()
    {
        var course = StoreCourse("a-medias", CourseLevel.Beginner, 5, 5, 5);
        _courses.Complete(EmployeeId, course.Lessons.First());

        var summary = (await ListAsync()).Value.ShouldHaveSingleItem();

        summary.CompletedLessons.ShouldBe(1);
        summary.ProgressPercentage.ShouldBe(33);
        summary.ProgressStatus.ShouldBe(CourseProgressStatus.InProgress);
    }

    [Fact]
    public async Task A_course_with_every_lesson_completed_is_completed()
    {
        var course = StoreCourse("terminado");
        foreach (var lesson in course.Lessons)
        {
            _courses.Complete(EmployeeId, lesson);
        }

        var summary = (await ListAsync()).Value.ShouldHaveSingleItem();

        summary.ProgressPercentage.ShouldBe(100);
        summary.ProgressStatus.ShouldBe(CourseProgressStatus.Completed);
    }

    [Fact]
    public async Task Another_employee_progress_does_not_count()
    {
        var course = StoreCourse("ajeno");
        _courses.Complete(Guid.CreateVersion7(), course.Lessons.First());

        (await ListAsync()).Value.ShouldHaveSingleItem().CompletedLessons.ShouldBe(0);
    }

    [Fact]
    public async Task Drafts_are_not_listed()
    {
        StoreCourse("publicado");
        StoreCourse("borrador", CourseLevel.Beginner, published: false);

        (await ListAsync()).Value.ShouldHaveSingleItem().Slug.ShouldBe("publicado");
    }

    [Fact]
    public async Task Courses_can_be_filtered_by_level()
    {
        StoreCourse("basico", CourseLevel.Beginner);
        StoreCourse("avanzado", CourseLevel.Advanced);

        (await ListAsync(level: CourseLevel.Advanced)).Value.ShouldHaveSingleItem().Slug.ShouldBe("avanzado");
    }

    [Fact]
    public async Task The_duration_filter_includes_courses_that_last_exactly_the_limit()
    {
        StoreCourse("corto", CourseLevel.Beginner, 20, 20);
        StoreCourse("largo", CourseLevel.Beginner, 30, 30);

        (await ListAsync(maxDuration: 40)).Value.ShouldHaveSingleItem().Slug.ShouldBe("corto");
    }

    [Fact]
    public async Task Courses_can_be_filtered_by_progress_status()
    {
        var started = StoreCourse("empezado", CourseLevel.Beginner, 5, 5);
        StoreCourse("nuevo");
        _courses.Complete(EmployeeId, started.Lessons.First());

        (await ListAsync(status: CourseProgressStatus.InProgress)).Value
            .ShouldHaveSingleItem().Slug.ShouldBe("empezado");
        (await ListAsync(status: CourseProgressStatus.NotStarted)).Value
            .ShouldHaveSingleItem().Slug.ShouldBe("nuevo");
    }

    [Fact]
    public async Task Courses_are_listed_from_beginner_to_advanced()
    {
        StoreCourse("avanzado", CourseLevel.Advanced);
        StoreCourse("basico", CourseLevel.Beginner);
        StoreCourse("intermedio", CourseLevel.Intermediate);

        (await ListAsync()).Value.Select(course => course.Slug)
            .ShouldBe(SlugsByLevel);
    }

    [Fact]
    public async Task Each_course_shows_its_average_rating_and_the_rating_of_the_employee()
    {
        var rated = StoreCourse("calificado");
        StoreCourse("sin-calificar");
        _ratings.Add(CourseRating.Create(EmployeeId, rated.Id, 5, Now));
        _ratings.Add(CourseRating.Create(Guid.CreateVersion7(), rated.Id, 2, Now));

        var courses = (await ListAsync()).Value;

        var withRatings = courses.Single(course => course.Slug == "calificado");
        withRatings.AverageRating.ShouldBe(3.5m);
        withRatings.RatingCount.ShouldBe(2);
        withRatings.MyRating.ShouldBe(5);
        var withoutRatings = courses.Single(course => course.Slug == "sin-calificar");
        withoutRatings.AverageRating.ShouldBeNull();
        withoutRatings.RatingCount.ShouldBe(0);
        withoutRatings.MyRating.ShouldBeNull();
    }
}

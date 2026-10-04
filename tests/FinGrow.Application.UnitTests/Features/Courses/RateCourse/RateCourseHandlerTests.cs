namespace FinGrow.Application.UnitTests.Features.Courses.RateCourse;

using Common;
using DTOs;
using FinGrow.Application.Features.Courses.RateCourse;
using Fakes;
using Domain.Entities;
using Domain.Enums;

public class RateCourseHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeCourseRepository _courses = new();
    private readonly FakeCourseRatingRepository _ratings = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    private Course StoreCourse()
    {
        var course = Course.Create(
            "fundamentos-finanzas-personales",
            "Fundamentos de Finanzas Personales",
            "Una descripcion corta.",
            CourseLevel.Beginner,
            EducationCategory.Basics,
            relatedInvestmentType: null,
            Now);

        course.AddLesson("Introduccion", 10, "https://www.youtube.com/embed/a", Now);
        course.AddLesson("Presupuesto", 15, "https://www.youtube.com/embed/b", Now);
        course.Publish(Now);

        _courses.Add(course);
        return course;
    }

    private Course StoreCompletedCourse()
    {
        var course = StoreCourse();

        foreach (var lesson in course.Lessons)
        {
            _courses.Complete(EmployeeId, lesson);
        }

        return course;
    }

    private Task<Result<CourseDetailResponse>> RateAsync(int score, string slug = "fundamentos-finanzas-personales") =>
        new RateCourseHandler(_courses, _ratings, _ratings, _unitOfWork, _clock)
            .Handle(new RateCourseCommand(EmployeeId, slug, score), CancellationToken.None);

    [Fact]
    public async Task Rating_a_completed_course_saves_it_and_shows_it_in_the_average()
    {
        var course = StoreCompletedCourse();

        var result = await RateAsync(4);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MyRating.ShouldBe(4);
        result.Value.RatingCount.ShouldBe(1);
        result.Value.AverageRating.ShouldBe(4m);
        var rating = _ratings.Ratings.ShouldHaveSingleItem();
        rating.EmployeeId.ShouldBe(EmployeeId);
        rating.CourseId.ShouldBe(course.Id);
        rating.RatedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_average_includes_the_ratings_of_other_employees_rounded_to_one_decimal()
    {
        var course = StoreCompletedCourse();
        _ratings.Add(CourseRating.Create(Guid.CreateVersion7(), course.Id, 5, Now));
        _ratings.Add(CourseRating.Create(Guid.CreateVersion7(), course.Id, 4, Now));

        var result = await RateAsync(4);

        result.Value.RatingCount.ShouldBe(3);
        result.Value.AverageRating.ShouldBe(4.3m);
        result.Value.MyRating.ShouldBe(4);
    }

    [Fact]
    public async Task Rating_again_changes_the_score_instead_of_adding_another_rating()
    {
        StoreCompletedCourse();
        await RateAsync(2);
        _clock.UtcNow = Now.AddDays(1);

        var result = await RateAsync(5);

        result.Value.MyRating.ShouldBe(5);
        result.Value.RatingCount.ShouldBe(1);
        var rating = _ratings.Ratings.ShouldHaveSingleItem();
        rating.Score.ShouldBe(5);
        rating.RatedAt.ShouldBe(Now);
        rating.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public async Task A_course_that_is_not_completed_cannot_be_rated()
    {
        var course = StoreCourse();
        _courses.Complete(EmployeeId, course.Lessons.First());

        var result = await RateAsync(5);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Course.NotCompleted");
        _ratings.Ratings.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_missing_course_is_not_found()
    {
        StoreCompletedCourse();

        var result = await RateAsync(5, slug: "no-existe");

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _ratings.Ratings.ShouldBeEmpty();
    }
}

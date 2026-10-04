namespace FinGrow.Application.UnitTests.Features.Courses.CompleteLesson;

using Common;
using DTOs;
using FinGrow.Application.Features.Courses.CompleteLesson;
using Fakes;
using Domain.Entities;
using Domain.Enums;

public class CompleteLessonHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeCourseRepository _courses = new();
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

    private Task<Result<CourseDetailResponse>> CompleteAsync(Guid lessonId, string slug = "fundamentos-finanzas-personales") =>
        new CompleteLessonHandler(_courses, _unitOfWork, _clock)
            .Handle(new CompleteLessonCommand(EmployeeId, slug, lessonId), CancellationToken.None);

    [Fact]
    public async Task Completing_a_lesson_persists_it_and_updates_the_progress()
    {
        var course = StoreCourse();
        var first = course.Lessons.First();

        var result = await CompleteAsync(first.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CompletedLessons.ShouldBe(1);
        result.Value.ProgressPercentage.ShouldBe(50);
        result.Value.ProgressStatus.ShouldBe(CourseProgressStatus.InProgress);
        result.Value.ResumeLessonId.ShouldBe(course.Lessons.Last().Id);
        var completion = _courses.Completions.ShouldHaveSingleItem();
        completion.EmployeeId.ShouldBe(EmployeeId);
        completion.LessonId.ShouldBe(first.Id);
        completion.CompletedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Completing_the_last_lesson_completes_the_course()
    {
        var course = StoreCourse();
        _courses.Complete(EmployeeId, course.Lessons.First());

        var result = await CompleteAsync(course.Lessons.Last().Id);

        result.Value.ProgressPercentage.ShouldBe(100);
        result.Value.ProgressStatus.ShouldBe(CourseProgressStatus.Completed);
    }

    [Fact]
    public async Task Completing_a_lesson_twice_does_not_duplicate_it()
    {
        var course = StoreCourse();
        var first = course.Lessons.First();
        _courses.Complete(EmployeeId, first);

        var result = await CompleteAsync(first.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CompletedLessons.ShouldBe(1);
        _courses.Completions.ShouldHaveSingleItem();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_lesson_of_another_course_is_not_found()
    {
        StoreCourse();

        var result = await CompleteAsync(Guid.CreateVersion7());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Course.LessonNotFound");
        _courses.Completions.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_missing_course_is_not_found()
    {
        var course = StoreCourse();

        var result = await CompleteAsync(course.Lessons.First().Id, slug: "no-existe");

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _courses.Completions.ShouldBeEmpty();
    }
}

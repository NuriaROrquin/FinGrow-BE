namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public class CourseTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly int[] ExpectedPositions = { 1, 2, 3 };
    private const string VideoUrl = "https://www.youtube.com/embed/9sCVcWD1Svs";

    [Fact]
    public void A_course_is_created_as_a_draft_without_lessons()
    {
        var course = CreateCourse();

        course.IsPublished.ShouldBeFalse();
        course.Lessons.ShouldBeEmpty();
        course.DurationMinutes.ShouldBe(0);
    }

    [Fact]
    public void Lessons_are_numbered_in_the_order_they_are_added()
    {
        var course = CreateCourse();

        course.AddLesson("Introducción", 10, VideoUrl, Now);
        course.AddLesson("Presupuesto", 15, VideoUrl, Now);
        course.AddLesson("Ahorro", 12, VideoUrl, Now);

        course.Lessons.Select(lesson => lesson.Position).ShouldBe(ExpectedPositions);
        course.Lessons.ShouldAllBe(lesson => lesson.CourseId == course.Id);
    }

    [Fact]
    public void The_duration_is_the_sum_of_the_lessons()
    {
        var course = CreateCourse();

        course.AddLesson("Introducción", 10, VideoUrl, Now);
        course.AddLesson("Presupuesto", 15, VideoUrl, Now);

        course.DurationMinutes.ShouldBe(25);
    }

    [Fact]
    public void A_course_without_lessons_cannot_be_published()
    {
        var course = CreateCourse();

        Should.Throw<DomainException>(() => course.Publish(Now));
    }

    [Fact]
    public void Publishing_again_keeps_the_original_publication_date()
    {
        var course = CreateCourse();
        course.AddLesson("Introducción", 10, VideoUrl, Now);

        course.Publish(Now);
        course.Publish(Now.AddDays(5));

        course.IsPublished.ShouldBeTrue();
        course.PublishedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Con Mayusculas")]
    [InlineData("con espacios")]
    [InlineData("-empieza-con-guion")]
    [InlineData("doble--guion")]
    public void An_invalid_slug_is_rejected(string slug)
    {
        Should.Throw<DomainException>(() =>
            Course.Create(slug, "Curso", "Descripción", CourseLevel.Beginner, EducationCategory.Basics, null, Now));
    }

    [Fact]
    public void A_title_is_required()
    {
        Should.Throw<DomainException>(() =>
            Course.Create("curso", "  ", "Descripción", CourseLevel.Beginner, EducationCategory.Basics, null, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_lesson_must_last_more_than_zero_minutes(int durationMinutes)
    {
        var course = CreateCourse();

        Should.Throw<DomainException>(() => course.AddLesson("Introducción", durationMinutes, VideoUrl, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-una-url")]
    [InlineData("ftp://videos.com/leccion")]
    public void A_lesson_needs_a_valid_http_video_url(string videoUrl)
    {
        var course = CreateCourse();

        Should.Throw<DomainException>(() => course.AddLesson("Introducción", 10, videoUrl, Now));
    }

    [Fact]
    public void A_course_can_be_related_to_an_investment_type()
    {
        var course = Course.Create(
            "estrategias-de-inversion-101",
            "Estrategias de Inversión 101",
            "Acciones, bonos y ETFs.",
            CourseLevel.Intermediate,
            EducationCategory.Investments,
            InvestmentType.Etf,
            Now);

        course.RelatedInvestmentType.ShouldBe(InvestmentType.Etf);
    }

    private static Course CreateCourse() =>
        Course.Create(
            "fundamentos-finanzas-personales",
            "Fundamentos de Finanzas Personales",
            "Aprendé a gestionar tu dinero.",
            CourseLevel.Beginner,
            EducationCategory.Basics,
            null,
            Now);
}

namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.ValueObjects;

public class LearningProgressTests
{
    [Fact]
    public void Courses_are_counted_by_their_progress_status()
    {
        var progress = LearningProgress.From(new[]
        {
            new CourseProgress(2, 2),
            new CourseProgress(3, 3),
            new CourseProgress(1, 4),
            new CourseProgress(0, 5),
        });

        progress.CourseCount.ShouldBe(4);
        progress.CompletedCourses.ShouldBe(2);
        progress.InProgressCourses.ShouldBe(1);
        progress.NotStartedCourses.ShouldBe(1);
    }

    [Fact]
    public void The_percentage_weighs_each_course_by_its_lessons_rounding_down()
    {
        var progress = LearningProgress.From(new[]
        {
            new CourseProgress(2, 2),
            new CourseProgress(0, 10),
        });

        progress.CompletedLessons.ShouldBe(2);
        progress.LessonCount.ShouldBe(12);
        progress.Percentage.ShouldBe(16);
    }

    [Fact]
    public void Without_courses_the_progress_is_zero()
    {
        var progress = LearningProgress.From(Array.Empty<CourseProgress>());

        progress.CourseCount.ShouldBe(0);
        progress.Percentage.ShouldBe(0);
    }
}

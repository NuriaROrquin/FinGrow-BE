namespace FinGrow.Application.UnitTests.Features.Courses;

using FinGrow.Application.Features.Courses.ListCourses;
using Domain.Enums;

public class ListCoursesQueryValidatorTests
{
    private readonly ListCoursesQueryValidator _validator = new();

    private static ListCoursesQuery Query(
        CourseLevel? level = null,
        int? maxDuration = null,
        CourseProgressStatus? status = null) =>
        new(Guid.CreateVersion7(), level, maxDuration, status);

    [Fact]
    public void A_query_without_filters_is_valid() =>
        _validator.Validate(Query()).IsValid.ShouldBeTrue();

    [Fact]
    public void A_query_with_every_filter_is_valid() =>
        _validator.Validate(Query(CourseLevel.Beginner, 45, CourseProgressStatus.InProgress)).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void The_maximum_duration_has_to_be_positive(int maxDuration) =>
        _validator.Validate(Query(maxDuration: maxDuration)).IsValid.ShouldBeFalse();

    [Fact]
    public void An_unknown_level_is_rejected() =>
        _validator.Validate(Query(level: (CourseLevel)99)).IsValid.ShouldBeFalse();

    [Fact]
    public void An_unknown_progress_status_is_rejected() =>
        _validator.Validate(Query(status: (CourseProgressStatus)99)).IsValid.ShouldBeFalse();
}

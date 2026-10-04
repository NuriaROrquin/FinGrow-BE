namespace FinGrow.Application.UnitTests.Validations.Courses;

using FinGrow.Application.Features.Courses.RateCourse;

public class RateCourseCommandValidatorTests
{
    private readonly RateCourseCommandValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void A_score_from_one_to_five_is_valid(int score)
    {
        _validator.Validate(new RateCourseCommand(Guid.CreateVersion7(), "curso", score)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-3)]
    public void A_score_outside_one_to_five_is_rejected(int score)
    {
        var result = _validator.Validate(new RateCourseCommand(Guid.CreateVersion7(), "curso", score));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(RateCourseCommand.Score));
    }
}

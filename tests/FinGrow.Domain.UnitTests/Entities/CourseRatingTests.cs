namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Errors;

public class CourseRatingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly Guid CourseId = Guid.CreateVersion7();

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void A_rating_between_one_and_five_is_accepted(int score)
    {
        var rating = CourseRating.Create(EmployeeId, CourseId, score, Now);

        rating.EmployeeId.ShouldBe(EmployeeId);
        rating.CourseId.ShouldBe(CourseId);
        rating.Score.ShouldBe(score);
        rating.RatedAt.ShouldBe(Now);
        rating.UpdatedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void A_rating_outside_one_to_five_is_rejected(int score)
    {
        Should.Throw<DomainException>(() => CourseRating.Create(EmployeeId, CourseId, score, Now));
    }

    [Fact]
    public void Changing_the_score_keeps_the_original_rating_date()
    {
        var rating = CourseRating.Create(EmployeeId, CourseId, 3, Now);

        rating.ChangeScore(5, Now.AddDays(2));

        rating.Score.ShouldBe(5);
        rating.RatedAt.ShouldBe(Now);
        rating.UpdatedAt.ShouldBe(Now.AddDays(2));
    }

    [Fact]
    public void A_new_score_outside_one_to_five_is_rejected()
    {
        var rating = CourseRating.Create(EmployeeId, CourseId, 3, Now);

        Should.Throw<DomainException>(() => rating.ChangeScore(0, Now));
        rating.Score.ShouldBe(3);
    }
}

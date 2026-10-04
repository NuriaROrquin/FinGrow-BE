namespace FinGrow.Application.UnitTests.Validations.Articles;

using FinGrow.Application.Features.Articles.ListArticles;
using Domain.Enums;

public class ListArticlesQueryValidatorTests
{
    private readonly ListArticlesQueryValidator _validator = new();

    [Fact]
    public void A_query_without_filters_passes()
    {
        _validator.Validate(new ListArticlesQuery(null, null)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_query_with_category_and_reading_time_passes()
    {
        _validator.Validate(new ListArticlesQuery(EducationCategory.Savings, 5)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_non_positive_reading_time_is_rejected(int minutes)
    {
        _validator.Validate(new ListArticlesQuery(null, minutes)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_unknown_category_is_rejected()
    {
        _validator.Validate(new ListArticlesQuery((EducationCategory)99, null)).IsValid.ShouldBeFalse();
    }
}

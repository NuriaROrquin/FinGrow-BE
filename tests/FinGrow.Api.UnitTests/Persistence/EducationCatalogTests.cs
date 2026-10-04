namespace FinGrow.Api.UnitTests.Persistence;

using FinGrow.Domain.Enums;
using FinGrow.Infrastructure.Persistence.Seeding;

public class EducationCatalogTests
{
    [Fact]
    public void Every_course_builds_as_a_published_course_with_ordered_lessons()
    {
        foreach (var seed in EducationCatalog.Courses)
        {
            var course = EducationCatalogSeeder.ToCourse(seed);

            course.IsPublished.ShouldBeTrue();
            course.Lessons.Count.ShouldBe(seed.Lessons.Count);
            course.Lessons.Select(lesson => lesson.Position)
                .ShouldBe(Enumerable.Range(1, seed.Lessons.Count));
        }
    }

    [Fact]
    public void Every_article_builds_as_a_published_article()
    {
        foreach (var seed in EducationCatalog.Articles)
        {
            var article = EducationCatalogSeeder.ToArticle(seed);

            article.IsPublished.ShouldBeTrue();
            article.ReadingTimeMinutes.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public void Slugs_are_unique_because_they_are_what_makes_the_load_idempotent()
    {
        EducationCatalog.Courses.Select(course => course.Slug).ShouldBeUnique();
        EducationCatalog.Articles.Select(article => article.Slug).ShouldBeUnique();
    }

    [Fact]
    public void A_first_load_adds_the_whole_catalog()
    {
        EducationCatalogSeeder.MissingCourses(Array.Empty<string>()).Count.ShouldBe(EducationCatalog.Courses.Length);
        EducationCatalogSeeder.MissingArticles(Array.Empty<string>()).Count.ShouldBe(EducationCatalog.Articles.Length);
    }

    [Fact]
    public void A_second_load_adds_nothing()
    {
        var courseSlugs = EducationCatalog.Courses.Select(course => course.Slug).ToList();
        var articleSlugs = EducationCatalog.Articles.Select(article => article.Slug).ToList();

        EducationCatalogSeeder.MissingCourses(courseSlugs).ShouldBeEmpty();
        EducationCatalogSeeder.MissingArticles(articleSlugs).ShouldBeEmpty();
    }

    [Fact]
    public void A_load_after_new_content_was_added_to_the_catalog_adds_only_the_new_content()
    {
        var courseSlugs = EducationCatalog.Courses.Skip(1).Select(course => course.Slug).ToList();
        var articleSlugs = EducationCatalog.Articles.Skip(1).Select(article => article.Slug).ToList();

        EducationCatalogSeeder.MissingCourses(courseSlugs).Select(course => course.Slug)
            .ShouldBe(new[] { EducationCatalog.Courses[0].Slug });
        EducationCatalogSeeder.MissingArticles(articleSlugs).Select(article => article.Slug)
            .ShouldBe(new[] { EducationCatalog.Articles[0].Slug });
    }

    [Fact]
    public void Every_investment_type_has_related_content_for_the_investments_screen()
    {
        var relatedTypes = EducationCatalog.Courses.Select(course => course.RelatedInvestmentType)
            .Concat(EducationCatalog.Articles.Select(article => article.RelatedInvestmentType))
            .OfType<InvestmentType>()
            .Distinct();

        relatedTypes.ShouldBe(Enum.GetValues<InvestmentType>(), ignoreOrder: true);
    }
}

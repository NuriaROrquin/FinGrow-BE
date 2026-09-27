namespace FinGrow.Api.UnitTests.Persistence;

using FinGrow.Infrastructure.Persistence.Seeding;

// La carga contra la base se probo a mano (dos corridas seguidas); aca se verifica que el
// catalogo en si sea cargable, que es lo que rompe si alguien agrega contenido invalido.
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
    public void There_is_content_related_to_investment_types_for_the_investments_screen()
    {
        var relatedTypes = EducationCatalog.Courses.Select(course => course.RelatedInvestmentType)
            .Concat(EducationCatalog.Articles.Select(article => article.RelatedInvestmentType))
            .OfType<Domain.Enums.InvestmentType>()
            .Distinct();

        relatedTypes.ShouldNotBeEmpty();
    }
}

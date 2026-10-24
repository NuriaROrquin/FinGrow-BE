namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Events;

public class ArticleTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_article_is_created_as_a_draft_with_its_category_and_reading_time()
    {
        var article = CreateArticle(readingTimeMinutes: 4);

        article.IsPublished.ShouldBeFalse();
        article.Category.ShouldBe(EducationCategory.Savings);
        article.ReadingTimeMinutes.ShouldBe(4);
    }

    [Fact]
    public void Publishing_again_keeps_the_original_publication_date()
    {
        var article = CreateArticle();

        article.Publish(Now);
        article.Publish(Now.AddDays(5));

        article.IsPublished.ShouldBeTrue();
        article.PublishedAt.ShouldBe(Now);
    }

    [Fact]
    public void Publishing_for_the_first_time_raises_the_event_and_publishing_again_does_not()
    {
        var article = CreateArticle();

        article.Publish(Now);
        article.Publish(Now.AddDays(5));

        var published = article.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<EducationContentPublished>();
        published.ContentId.ShouldBe(article.Id);
        published.Kind.ShouldBe(EducationContentKind.Article);
        published.Title.ShouldBe(article.Title);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void The_reading_time_must_be_greater_than_zero(int readingTimeMinutes)
    {
        Should.Throw<DomainException>(() => CreateArticle(readingTimeMinutes));
    }

    [Fact]
    public void The_content_is_required()
    {
        Should.Throw<DomainException>(() =>
            Article.Create("fondo-de-emergencia", "Fondo", "Resumen", " ", EducationCategory.Savings, 4, null, Now));
    }

    [Fact]
    public void An_invalid_slug_is_rejected()
    {
        Should.Throw<DomainException>(() =>
            Article.Create("Fondo de emergencia", "Fondo", "Resumen", "Contenido", EducationCategory.Savings, 4, null, Now));
    }

    [Fact]
    public void An_article_about_an_investment_type_is_investment_content()
    {
        var article = Article.Create(
            "cedears-para-principiantes",
            "CEDEARs para principiantes",
            "Que son y como se compran.",
            "## Que es un CEDEAR",
            EducationCategory.Savings,
            6,
            InvestmentType.Cedear,
            Now);

        article.IsInvestmentContent.ShouldBeTrue();
    }

    [Fact]
    public void An_article_of_the_investments_category_is_investment_content_even_without_a_type()
    {
        var article = Article.Create(
            "diversificar",
            "Como diversificar",
            "No pongas todo en el mismo lugar.",
            "## Diversificar",
            EducationCategory.Investments,
            5,
            null,
            Now);

        article.IsInvestmentContent.ShouldBeTrue();
    }

    [Fact]
    public void An_article_unrelated_to_investments_is_not_investment_content()
    {
        CreateArticle().IsInvestmentContent.ShouldBeFalse();
    }

    private static Article CreateArticle(int readingTimeMinutes = 4) =>
        Article.Create(
            "fondo-de-emergencia",
            "5 formas de construir un fondo de emergencia",
            "Estrategias para juntar un colchón.",
            "## Automatizá el ahorro",
            EducationCategory.Savings,
            readingTimeMinutes,
            null,
            Now);
}

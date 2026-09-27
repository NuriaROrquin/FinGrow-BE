namespace FinGrow.Application.UnitTests.Features.Articles;

using Common;
using FinGrow.Application.Features.Articles.GetArticle;
using FinGrow.Application.Features.Articles.ListArticles;
using Fakes;
using Domain.Entities;
using Domain.Enums;

public class ArticleCatalogHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeArticleRepository _articles = new();

    private Article StoreArticle(
        string slug,
        EducationCategory category = EducationCategory.Savings,
        int readingTimeMinutes = 5,
        bool published = true,
        DateTimeOffset? publishedAt = null)
    {
        var article = Article.Create(
            slug,
            $"Titulo {slug}",
            "Un resumen corto.",
            "## Subtitulo\n\nCuerpo en **Markdown**.",
            category,
            readingTimeMinutes,
            relatedInvestmentType: null,
            Now);

        if (published)
        {
            article.Publish(publishedAt ?? Now);
        }

        _articles.Add(article);
        return article;
    }

    private Task<Result<IReadOnlyList<Application.DTOs.ArticleSummaryResponse>>> ListAsync(
        EducationCategory? category = null,
        int? maxReadingTime = null) =>
        new ListArticlesHandler(_articles).Handle(new ListArticlesQuery(category, maxReadingTime), CancellationToken.None);

    [Fact]
    public async Task The_list_shows_each_published_article_with_its_category_and_reading_time()
    {
        StoreArticle("fondo-de-emergencia", EducationCategory.Savings, readingTimeMinutes: 4);

        var article = (await ListAsync()).Value.ShouldHaveSingleItem();

        article.Slug.ShouldBe("fondo-de-emergencia");
        article.Category.ShouldBe(EducationCategory.Savings);
        article.ReadingTimeMinutes.ShouldBe(4);
        article.PublishedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Drafts_are_not_listed()
    {
        StoreArticle("publicado");
        StoreArticle("borrador", published: false);

        (await ListAsync()).Value.ShouldHaveSingleItem().Slug.ShouldBe("publicado");
    }

    [Fact]
    public async Task Articles_can_be_filtered_by_category()
    {
        StoreArticle("ahorro", EducationCategory.Savings);
        StoreArticle("credito", EducationCategory.Credit);

        (await ListAsync(category: EducationCategory.Credit)).Value.ShouldHaveSingleItem().Slug.ShouldBe("credito");
    }

    [Fact]
    public async Task Articles_can_be_filtered_by_maximum_reading_time_inclusive()
    {
        StoreArticle("corto", readingTimeMinutes: 3);
        StoreArticle("justo", readingTimeMinutes: 5);
        StoreArticle("largo", readingTimeMinutes: 12);

        var slugs = (await ListAsync(maxReadingTime: 5)).Value.Select(article => article.Slug);

        slugs.ShouldBe(["corto", "justo"], ignoreOrder: true);
    }

    [Fact]
    public async Task The_newest_articles_come_first()
    {
        StoreArticle("viejo", publishedAt: Now.AddDays(-10));
        StoreArticle("nuevo", publishedAt: Now);

        (await ListAsync()).Value.Select(article => article.Slug).ToList().ShouldBe(["nuevo", "viejo"]);
    }

    [Fact]
    public async Task Opening_an_article_returns_its_markdown_content()
    {
        StoreArticle("fondo-de-emergencia");

        var result = await new GetArticleHandler(_articles)
            .Handle(new GetArticleQuery("fondo-de-emergencia"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Content.ShouldBe("## Subtitulo\n\nCuerpo en **Markdown**.");
    }

    [Theory]
    [InlineData("no-existe")]
    [InlineData("borrador")]
    public async Task A_missing_or_draft_article_is_not_found(string slug)
    {
        StoreArticle("borrador", published: false);

        var result = await new GetArticleHandler(_articles).Handle(new GetArticleQuery(slug), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}

namespace FinGrow.Application.UnitTests.Features.Articles.GetArticle;

using Common;
using FinGrow.Application.Features.Articles.GetArticle;
using Fakes;
using Domain.Entities;
using Domain.Enums;

public class GetArticleHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeArticleRepository _articles = new();

    private void StoreArticle(string slug, bool published = true)
    {
        var article = Article.Create(
            slug,
            $"Titulo {slug}",
            "Un resumen corto.",
            "## Subtitulo\n\nCuerpo en **Markdown**.",
            EducationCategory.Savings,
            readingTimeMinutes: 5,
            relatedInvestmentType: null,
            Now);

        if (published)
        {
            article.Publish(Now);
        }

        _articles.Add(article);
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

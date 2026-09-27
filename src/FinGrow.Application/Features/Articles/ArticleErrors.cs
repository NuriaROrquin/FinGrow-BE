namespace FinGrow.Application.Features.Articles;

using Common;

internal static class ArticleErrors
{
    public static Error NotFound(string slug) =>
        Error.NotFound("Article.NotFound", $"No existe un articulo publicado con slug '{slug}'.");
}

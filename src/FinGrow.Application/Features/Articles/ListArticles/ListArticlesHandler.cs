namespace FinGrow.Application.Features.Articles.ListArticles;

using Common;
using DTOs;
using Domain.Repositories;
using MediatR;

internal sealed class ListArticlesHandler(IArticleRepository articleRepository)
    : IRequestHandler<ListArticlesQuery, Result<IReadOnlyList<ArticleSummaryResponse>>>
{
    public async Task<Result<IReadOnlyList<ArticleSummaryResponse>>> Handle(
        ListArticlesQuery request,
        CancellationToken cancellationToken)
    {
        var articles = await articleRepository.ListPublishedAsync(
            request.Category,
            request.MaxReadingTimeMinutes,
            cancellationToken);

        return Result.Success<IReadOnlyList<ArticleSummaryResponse>>(
            articles.Select(ArticleSummaryResponse.FromEntity).ToList());
    }
}

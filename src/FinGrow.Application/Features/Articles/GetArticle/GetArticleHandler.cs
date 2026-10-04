namespace FinGrow.Application.Features.Articles.GetArticle;

using Common;
using DTOs;
using Domain.Repositories;
using MediatR;

internal sealed class GetArticleHandler(IArticleRepository articleRepository)
    : IRequestHandler<GetArticleQuery, Result<ArticleResponse>>
{
    public async Task<Result<ArticleResponse>> Handle(GetArticleQuery request, CancellationToken cancellationToken)
    {
        var article = await articleRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);

        return article is null
            ? Result.Failure<ArticleResponse>(ArticleErrors.NotFound(request.Slug))
            : Result.Success(ArticleResponse.FromEntity(article));
    }
}

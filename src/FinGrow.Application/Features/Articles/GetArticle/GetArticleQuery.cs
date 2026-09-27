namespace FinGrow.Application.Features.Articles.GetArticle;

using Common;
using DTOs;
using MediatR;

public sealed record GetArticleQuery(string Slug) : IRequest<Result<ArticleResponse>>;

namespace FinGrow.Application.Features.Articles.ListArticles;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

public sealed record ListArticlesQuery(EducationCategory? Category, int? MaxReadingTimeMinutes)
    : IRequest<Result<IReadOnlyList<ArticleSummaryResponse>>>;

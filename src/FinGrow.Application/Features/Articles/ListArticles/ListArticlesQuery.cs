namespace FinGrow.Application.Features.Articles.ListArticles;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

/// <param name="MaxReadingTimeMinutes">Solo los articulos que se leen en ese tiempo o menos.</param>
public sealed record ListArticlesQuery(EducationCategory? Category, int? MaxReadingTimeMinutes)
    : IRequest<Result<IReadOnlyList<ArticleSummaryResponse>>>;

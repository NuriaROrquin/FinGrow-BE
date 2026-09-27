namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

/// <summary>Un articulo abierto para leer. <see cref="Content"/> es Markdown.</summary>
public sealed record ArticleResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Content,
    EducationCategory Category,
    int ReadingTimeMinutes,
    InvestmentType? RelatedInvestmentType,
    DateTimeOffset PublishedAt)
{
    public static ArticleResponse FromEntity(Article article) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.Content,
        article.Category,
        article.ReadingTimeMinutes,
        article.RelatedInvestmentType,
        article.PublishedAt!.Value);
}

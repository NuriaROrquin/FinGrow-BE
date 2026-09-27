namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

/// <summary>Un articulo en el listado del catalogo: sin el cuerpo, que solo viaja al abrirlo.</summary>
public sealed record ArticleSummaryResponse(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    EducationCategory Category,
    int ReadingTimeMinutes,
    InvestmentType? RelatedInvestmentType,
    DateTimeOffset PublishedAt)
{
    public static ArticleSummaryResponse FromEntity(Article article) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.Category,
        article.ReadingTimeMinutes,
        article.RelatedInvestmentType,
        article.PublishedAt!.Value);
}

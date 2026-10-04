namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class Article : AggregateRoot
{
    public const int MaxTitleLength = 200;
    public const int MaxSummaryLength = 500;
    public const int MaxContentLength = 50_000;

    private Article()
    {
    }

    private Article(
        Guid id,
        string slug,
        string title,
        string summary,
        string content,
        EducationCategory category,
        int readingTimeMinutes,
        InvestmentType? relatedInvestmentType,
        DateTimeOffset createdAt)
        : base(id)
    {
        Slug = slug;
        Title = title;
        Summary = summary;
        Content = content;
        Category = category;
        ReadingTimeMinutes = readingTimeMinutes;
        RelatedInvestmentType = relatedInvestmentType;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public string Slug { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public EducationCategory Category { get; private set; }

    public int ReadingTimeMinutes { get; private set; }

    public InvestmentType? RelatedInvestmentType { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsPublished => PublishedAt is not null;

    public static Article Create(
        string slug,
        string title,
        string summary,
        string content,
        EducationCategory category,
        int readingTimeMinutes,
        InvestmentType? relatedInvestmentType,
        DateTimeOffset createdAt)
    {
        if (readingTimeMinutes <= 0)
        {
            throw new DomainException("El tiempo de lectura tiene que ser mayor a cero.");
        }

        return new Article(
            Guid.CreateVersion7(),
            Common.Slug.EnsureValid(slug),
            RequiredText.Ensure(title, MaxTitleLength, "El titulo del articulo"),
            RequiredText.Ensure(summary, MaxSummaryLength, "El resumen del articulo"),
            RequiredText.Ensure(content, MaxContentLength, "El contenido del articulo"),
            category,
            readingTimeMinutes,
            relatedInvestmentType,
            createdAt);
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        PublishedAt ??= publishedAt;
        UpdatedAt = publishedAt;
    }
}

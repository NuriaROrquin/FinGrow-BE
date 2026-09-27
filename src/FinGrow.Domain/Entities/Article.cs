namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

/// <summary>
/// Un articulo del catalogo de educacion financiera, pensado para leerse en un rato corto
/// (HU-38). El cuerpo es Markdown: el frontend lo renderiza tal cual.
/// </summary>
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

    /// <summary>Tipo de activo del que trata el articulo, si trata de uno (HU-35).</summary>
    public InvestmentType? RelatedInvestmentType { get; private set; }

    /// <summary>Mientras es <c>null</c> el articulo es un borrador y no se muestra en el catalogo.</summary>
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

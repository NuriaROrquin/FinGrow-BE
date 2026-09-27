namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

/// <summary>
/// Un curso del catalogo de educacion financiera. Es contenido de la plataforma, no de una
/// empresa ni de un empleado: lo ven todos. El progreso de cada empleado vive aparte.
/// </summary>
public sealed class Course : AggregateRoot
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 1000;

    private readonly List<Lesson> _lessons = new();

    private Course()
    {
    }

    private Course(
        Guid id,
        string slug,
        string title,
        string description,
        CourseLevel level,
        EducationCategory category,
        InvestmentType? relatedInvestmentType,
        DateTimeOffset createdAt)
        : base(id)
    {
        Slug = slug;
        Title = title;
        Description = description;
        Level = level;
        Category = category;
        RelatedInvestmentType = relatedInvestmentType;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public string Slug { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public CourseLevel Level { get; private set; }

    public EducationCategory Category { get; private set; }

    /// <summary>
    /// Tipo de activo del que trata el curso, si trata de uno. Es lo que usa Inversiones para
    /// sugerir material relacionado (HU-35).
    /// </summary>
    public InvestmentType? RelatedInvestmentType { get; private set; }

    /// <summary>Mientras es <c>null</c> el curso es un borrador y no se muestra en el catalogo.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Lesson> Lessons => _lessons.AsReadOnly();

    public bool IsPublished => PublishedAt is not null;

    /// <summary>Se deriva de las lecciones: una duracion guardada se desincroniza al editarlas.</summary>
    public int DurationMinutes => _lessons.Sum(lesson => lesson.DurationMinutes);

    public static Course Create(
        string slug,
        string title,
        string description,
        CourseLevel level,
        EducationCategory category,
        InvestmentType? relatedInvestmentType,
        DateTimeOffset createdAt) =>
        new(Guid.CreateVersion7(),
            Common.Slug.EnsureValid(slug),
            RequiredText.Ensure(title, MaxTitleLength, "El titulo del curso"),
            RequiredText.Ensure(description, MaxDescriptionLength, "La descripcion del curso"),
            level,
            category,
            relatedInvestmentType,
            createdAt);

    /// <summary>Agrega la leccion al final del curso.</summary>
    public Lesson AddLesson(string title, int durationMinutes, string videoUrl, DateTimeOffset updatedAt)
    {
        var lesson = Lesson.Create(Id, _lessons.Count + 1, title, durationMinutes, videoUrl);
        _lessons.Add(lesson);
        UpdatedAt = updatedAt;

        return lesson;
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        if (_lessons.Count == 0)
        {
            throw new DomainException("No se puede publicar un curso sin lecciones.");
        }

        PublishedAt ??= publishedAt;
        UpdatedAt = publishedAt;
    }
}

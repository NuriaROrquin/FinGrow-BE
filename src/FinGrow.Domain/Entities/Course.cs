namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

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

    public InvestmentType? RelatedInvestmentType { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Lesson> Lessons => _lessons.OrderBy(lesson => lesson.Position).ToList().AsReadOnly();

    public bool IsPublished => PublishedAt is not null;

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

    public Lesson AddLesson(string title, int durationMinutes, string videoUrl, DateTimeOffset updatedAt)
    {
        var lesson = Lesson.Create(Id, _lessons.Count + 1, title, durationMinutes, videoUrl);
        _lessons.Add(lesson);
        UpdatedAt = updatedAt;

        return lesson;
    }

    public Lesson? FindLesson(Guid lessonId) => _lessons.FirstOrDefault(lesson => lesson.Id == lessonId);

    public CourseProgress ProgressFor(IReadOnlySet<Guid> completedLessonIds) =>
        new(_lessons.Count(lesson => completedLessonIds.Contains(lesson.Id)), _lessons.Count);

    public Lesson? ResumeLessonFor(IReadOnlySet<Guid> completedLessonIds) =>
        Lessons.FirstOrDefault(lesson => !completedLessonIds.Contains(lesson.Id)) ?? Lessons.FirstOrDefault();

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

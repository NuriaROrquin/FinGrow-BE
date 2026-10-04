namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

public sealed class Lesson : Entity
{
    public const int MaxTitleLength = 200;
    public const int MaxVideoUrlLength = 500;

    private Lesson()
    {
    }

    private Lesson(Guid id, Guid courseId, int position, string title, int durationMinutes, string videoUrl)
        : base(id)
    {
        CourseId = courseId;
        Position = position;
        Title = title;
        DurationMinutes = durationMinutes;
        VideoUrl = videoUrl;
    }

    public Guid CourseId { get; private set; }

    public int Position { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public int DurationMinutes { get; private set; }

    public string VideoUrl { get; private set; } = string.Empty;

    internal static Lesson Create(Guid courseId, int position, string title, int durationMinutes, string videoUrl)
    {
        if (durationMinutes <= 0)
        {
            throw new DomainException("La duracion de la leccion tiene que ser mayor a cero.");
        }

        return new Lesson(
            Guid.CreateVersion7(),
            courseId,
            position,
            RequiredText.Ensure(title, MaxTitleLength, "El titulo de la leccion"),
            durationMinutes,
            EnsureValidVideoUrl(videoUrl));
    }

    private static string EnsureValidVideoUrl(string videoUrl)
    {
        var trimmed = (videoUrl ?? string.Empty).Trim();

        var isValid = trimmed.Length <= MaxVideoUrlLength
                      && Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
                      && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

        return isValid
            ? trimmed
            : throw new DomainException("La URL del video de la leccion no es valida.");
    }
}

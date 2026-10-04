namespace FinGrow.Domain.Repositories;

using Entities;
using Enums;

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> ListPublishedAsync(
        EducationCategory? category,
        int? maxReadingTimeMinutes,
        CancellationToken cancellationToken = default);

    Task<Article?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

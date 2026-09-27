namespace FinGrow.Domain.Repositories;

using Entities;
using Enums;

public interface IArticleRepository
{
    /// <summary>
    /// Los articulos publicados, del mas nuevo al mas viejo. Sin filtro de categoria o de tiempo
    /// de lectura devuelve todo el catalogo publicado.
    /// </summary>
    Task<IReadOnlyList<Article>> ListPublishedAsync(
        EducationCategory? category,
        int? maxReadingTimeMinutes,
        CancellationToken cancellationToken = default);

    Task<Article?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

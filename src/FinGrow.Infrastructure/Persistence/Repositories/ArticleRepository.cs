namespace FinGrow.Infrastructure.Persistence.Repositories;

using Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class ArticleRepository(FinGrowDbContext dbContext) : IArticleRepository
{
    public async Task<IReadOnlyList<Article>> ListPublishedAsync(
        EducationCategory? category,
        int? maxReadingTimeMinutes,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Articles
            .AsNoTracking()
            .Where(article => article.PublishedAt != null);

        if (category is not null)
        {
            query = query.Where(article => article.Category == category);
        }

        if (maxReadingTimeMinutes is not null)
        {
            query = query.Where(article => article.ReadingTimeMinutes <= maxReadingTimeMinutes);
        }

        return await query
            .OrderByDescending(article => article.PublishedAt)
            .ThenBy(article => article.Title)
            .ToListAsync(cancellationToken);
    }

    public Task<Article?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        dbContext.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(article => article.Slug == slug && article.PublishedAt != null, cancellationToken);
}

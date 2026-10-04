namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

internal sealed class CourseRatingReadRepository(FinGrowDbContext dbContext) : ICourseRatingReadRepository
{
    public async Task<IReadOnlyDictionary<Guid, CourseRatingSummary>> GetSummariesAsync(
        Guid employeeId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        var ids = courseIds.ToList();

        var totals = await dbContext.CourseRatings
            .AsNoTracking()
            .Where(rating => ids.Contains(rating.CourseId))
            .GroupBy(rating => rating.CourseId)
            .Select(group => new
            {
                CourseId = group.Key,
                Count = group.Count(),
                Average = group.Average(rating => (decimal)rating.Score),
            })
            .ToListAsync(cancellationToken);

        var employeeScores = await dbContext.CourseRatings
            .AsNoTracking()
            .Where(rating => rating.EmployeeId == employeeId && ids.Contains(rating.CourseId))
            .ToDictionaryAsync(rating => rating.CourseId, rating => rating.Score, cancellationToken);

        return totals.ToDictionary(
            total => total.CourseId,
            total => new CourseRatingSummary(
                total.Count,
                total.Average,
                employeeScores.TryGetValue(total.CourseId, out var score) ? score : null));
    }
}

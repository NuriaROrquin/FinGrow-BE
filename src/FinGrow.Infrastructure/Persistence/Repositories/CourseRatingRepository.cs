namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class CourseRatingRepository(FinGrowDbContext dbContext) : ICourseRatingRepository
{
    public Task<CourseRating?> FindAsync(Guid employeeId, Guid courseId, CancellationToken cancellationToken = default) =>
        dbContext.CourseRatings
            .FirstOrDefaultAsync(rating => rating.EmployeeId == employeeId && rating.CourseId == courseId, cancellationToken);

    public void Add(CourseRating rating) => dbContext.CourseRatings.Add(rating);
}

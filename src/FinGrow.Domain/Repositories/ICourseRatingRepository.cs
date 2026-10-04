namespace FinGrow.Domain.Repositories;

using Entities;

public interface ICourseRatingRepository
{
    Task<CourseRating?> FindAsync(Guid employeeId, Guid courseId, CancellationToken cancellationToken = default);

    void Add(CourseRating rating);
}

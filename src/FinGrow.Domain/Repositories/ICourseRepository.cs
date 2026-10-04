namespace FinGrow.Domain.Repositories;

using Entities;
using Enums;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> ListPublishedAsync(
        CourseLevel? level,
        int? maxDurationMinutes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> ListCompletedLessonIdsAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

namespace FinGrow.Domain.Repositories;

using Entities;
using Enums;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> ListPublishedAsync(
        CourseLevel? level,
        int? maxDurationMinutes,
        CancellationToken cancellationToken = default);

    Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> ListCompletedLessonIdsAsync(Guid employeeId, CancellationToken cancellationToken = default);

    void AddCompletion(LessonCompletion completion);
}

namespace FinGrow.Infrastructure.Persistence.Repositories;

using Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class CourseRepository(FinGrowDbContext dbContext) : ICourseRepository
{
    public async Task<IReadOnlyList<Course>> ListPublishedAsync(
        CourseLevel? level,
        int? maxDurationMinutes,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Courses
            .AsNoTracking()
            .Where(course => course.PublishedAt != null);

        if (level is not null)
        {
            query = query.Where(course => course.Level == level);
        }

        if (maxDurationMinutes is not null)
        {
            // La duracion no esta guardada (se deriva de las lecciones): se suma en la base.
            query = query.Where(course => course.Lessons.Sum(lesson => lesson.DurationMinutes) <= maxDurationMinutes);
        }

        var courses = await query.ToListAsync(cancellationToken);

        // Level se guarda como texto, asi que ordenar en la base lo haria alfabeticamente.
        return courses
            .OrderBy(course => course.Level)
            .ThenBy(course => course.Title)
            .ToList();
    }

    public async Task<IReadOnlySet<Guid>> ListCompletedLessonIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var lessonIds = await dbContext.LessonCompletions
            .Where(completion => completion.EmployeeId == employeeId)
            .Select(completion => completion.LessonId)
            .ToListAsync(cancellationToken);

        return lessonIds.ToHashSet();
    }
}

namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class AchievementRepository(FinGrowDbContext dbContext) : IAchievementRepository
{
    public void Add(EmployeeAchievement achievement) => dbContext.EmployeeAchievements.Add(achievement);

    public async Task<IReadOnlyList<EmployeeAchievement>> ListByEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.EmployeeAchievements
            .AsNoTracking()
            .Where(achievement => achievement.EmployeeId == employeeId)
            .OrderBy(achievement => achievement.AchievedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EmployeeAchievement>> ListUncelebratedAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.EmployeeAchievements
            .Where(achievement => achievement.EmployeeId == employeeId && achievement.CelebratedAt == null)
            .OrderBy(achievement => achievement.AchievedAt)
            .ToListAsync(cancellationToken);
}

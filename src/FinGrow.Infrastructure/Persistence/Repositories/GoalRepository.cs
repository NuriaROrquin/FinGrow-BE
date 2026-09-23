namespace FinGrow.Infrastructure.Persistence.Repositories;

using Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class GoalRepository(FinGrowDbContext dbContext) : IGoalRepository
{
    public void Add(Goal goal) => dbContext.Goals.Add(goal);

    public Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Goals.FirstOrDefaultAsync(goal => goal.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Goal>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await dbContext.Goals
            .Where(goal => goal.EmployeeId == employeeId)
            .OrderByDescending(goal => goal.CreatedAt)
            .ToListAsync(cancellationToken);
}

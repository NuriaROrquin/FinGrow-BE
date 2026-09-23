namespace FinGrow.Infrastructure.Persistence.Repositories;

using Domain.Entities;
using FinGrow.Domain.Repositories;

internal sealed class GoalRepository(FinGrowDbContext dbContext) : IGoalRepository
{
    public void Add(Goal goal) => dbContext.Goals.Add(goal);
}

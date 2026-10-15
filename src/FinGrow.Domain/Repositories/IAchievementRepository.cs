namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IAchievementRepository
{
    void Add(EmployeeAchievement achievement);

    Task<IReadOnlyList<EmployeeAchievement>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeAchievement>> ListUncelebratedAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

namespace FinGrow.Domain.Repositories;

using Entities;

public interface IGoalRepository
{
    void Add(Goal goal);

    Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Goal>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

namespace FinGrow.Domain.Repositories;

using Entities;
using Enums;

public interface IInvestmentRepository
{
    void Add(Investment investment);

    void Remove(Investment investment);

    Task<Investment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Investment>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Los empleados con inversiones manuales con simbolo. Las importadas quedan afuera: las valua su integracion.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListEmployeesWithTrackedInvestmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Investment>> ListTrackedByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Investment>> ListImportedAsync(
        Guid employeeId,
        InvestmentSource source,
        CancellationToken cancellationToken = default);
}

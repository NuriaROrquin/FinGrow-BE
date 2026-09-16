namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public interface IEmployeeIntegrationRepository
{
    Task<EmployeeIntegration?> FindByExternalAccountAsync(
        IntegrationProvider provider,
        string externalAccountId,
        CancellationToken cancellationToken = default);

    Task<EmployeeIntegration?> FindByEmployeeAsync(
        Guid employeeId,
        IntegrationProvider provider,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeIntegration>> ListAuthorizedAsync(
        IntegrationProvider provider,
        CancellationToken cancellationToken = default);

    void Add(EmployeeIntegration integration);

    void Remove(EmployeeIntegration integration);
}

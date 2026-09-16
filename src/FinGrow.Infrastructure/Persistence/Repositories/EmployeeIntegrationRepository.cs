namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class EmployeeIntegrationRepository : IEmployeeIntegrationRepository
{
    private readonly FinGrowDbContext _dbContext;

    public EmployeeIntegrationRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public Task<EmployeeIntegration?> FindByExternalAccountAsync(
        IntegrationProvider provider,
        string externalAccountId,
        CancellationToken cancellationToken = default) =>
        _dbContext.EmployeeIntegrations.FirstOrDefaultAsync(
            integration => integration.Provider == provider && integration.ExternalAccountId == externalAccountId,
            cancellationToken);

    public Task<EmployeeIntegration?> FindByEmployeeAsync(
        Guid employeeId,
        IntegrationProvider provider,
        CancellationToken cancellationToken = default) =>
        _dbContext.EmployeeIntegrations.FirstOrDefaultAsync(
            integration => integration.EmployeeId == employeeId && integration.Provider == provider,
            cancellationToken);

    public async Task<IReadOnlyList<EmployeeIntegration>> ListAuthorizedAsync(
        IntegrationProvider provider,
        CancellationToken cancellationToken = default) =>
        await _dbContext.EmployeeIntegrations
            .Where(integration => integration.Provider == provider && integration.Grant != null)
            .OrderBy(integration => integration.LastSyncedAt)
            .ToListAsync(cancellationToken);

    public void Add(EmployeeIntegration integration) => _dbContext.EmployeeIntegrations.Add(integration);

    public void Remove(EmployeeIntegration integration) => _dbContext.EmployeeIntegrations.Remove(integration);
}

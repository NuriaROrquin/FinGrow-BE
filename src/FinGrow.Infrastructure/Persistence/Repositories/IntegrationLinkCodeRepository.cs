namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class IntegrationLinkCodeRepository : IIntegrationLinkCodeRepository
{
    private readonly FinGrowDbContext _dbContext;

    public IntegrationLinkCodeRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public Task<IntegrationLinkCode?> FindByHashAsync(
        IntegrationProvider provider,
        string codeHash,
        CancellationToken cancellationToken = default) =>
        _dbContext.IntegrationLinkCodes.FirstOrDefaultAsync(
            linkCode => linkCode.Provider == provider && linkCode.CodeHash == codeHash,
            cancellationToken);

    public void Add(IntegrationLinkCode linkCode) => _dbContext.IntegrationLinkCodes.Add(linkCode);
}

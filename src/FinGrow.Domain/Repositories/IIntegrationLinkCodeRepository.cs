namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public interface IIntegrationLinkCodeRepository
{
    Task<IntegrationLinkCode?> FindByHashAsync(
        IntegrationProvider provider,
        string codeHash,
        CancellationToken cancellationToken = default);

    void Add(IntegrationLinkCode linkCode);
}

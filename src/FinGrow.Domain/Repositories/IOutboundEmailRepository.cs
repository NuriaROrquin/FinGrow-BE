namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IOutboundEmailRepository
{
    void Add(OutboundEmail email);

    Task<OutboundEmail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListDueIdsAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default);
}

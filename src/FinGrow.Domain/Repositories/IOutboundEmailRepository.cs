namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IOutboundEmailRepository
{
    void Add(OutboundEmail email);

    Task<OutboundEmail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

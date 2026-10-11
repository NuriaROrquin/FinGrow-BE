namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class OutboundEmailRepository : IOutboundEmailRepository
{
    private readonly FinGrowDbContext _dbContext;

    public OutboundEmailRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public void Add(OutboundEmail email) => _dbContext.OutboundEmails.Add(email);

    public Task<OutboundEmail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.OutboundEmails.FirstOrDefaultAsync(email => email.Id == id, cancellationToken);
}

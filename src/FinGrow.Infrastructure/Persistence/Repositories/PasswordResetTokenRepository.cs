namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly FinGrowDbContext _dbContext;

    public PasswordResetTokenRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _dbContext.PasswordResetTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task<bool> AnyIssuedAfterAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
        _dbContext.PasswordResetTokens.AnyAsync(token => token.UserId == userId && token.CreatedAt > since, cancellationToken);

    public void Add(PasswordResetToken token) => _dbContext.PasswordResetTokens.Add(token);

    public Task InvalidateAllForUserAsync(Guid userId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) =>
        _dbContext.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.UsedAt, usedAt), cancellationToken);
}

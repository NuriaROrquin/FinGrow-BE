namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly FinGrowDbContext _dbContext;

    public RefreshTokenRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public void Add(RefreshToken refreshToken) => _dbContext.RefreshTokens.Add(refreshToken);

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, revokedAt), cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now)
            .OrderByDescending(token => token.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<RefreshToken?> FindActiveBySessionAsync(
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens
            .Where(token => token.SessionId == sessionId && token.RevokedAt == null && token.ExpiresAt > now)
            .OrderByDescending(token => token.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task RevokeAllForUserExceptSessionAsync(
        Guid userId,
        Guid keptSessionId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.SessionId != keptSessionId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, revokedAt), cancellationToken);
}

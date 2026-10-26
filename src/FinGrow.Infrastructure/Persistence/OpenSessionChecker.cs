namespace FinGrow.Infrastructure.Persistence;

using FinGrow.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

internal sealed class OpenSessionChecker : IOpenSessionChecker
{
    private readonly FinGrowDbContext _dbContext;
    private readonly IDateTimeProvider _clock;

    public OpenSessionChecker(FinGrowDbContext dbContext, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    // Corre mientras se valida el access token, antes de que el pedido tenga usuario: no depende de
    // si el filtro de empresa ve o no la fila, el usuario lo acota la consulta.
    public Task<bool> IsOpenAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        return _dbContext.RefreshTokens
            .IgnoreQueryFilters(new[] { FinGrowDbContext.TenantFilter })
            .AnyAsync(
                token => token.SessionId == sessionId
                    && token.UserId == userId
                    && token.RevokedAt == null
                    && token.ExpiresAt > now,
                cancellationToken);
    }
}

namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    void Add(RefreshToken refreshToken);
    
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);

    /// <summary>El token vigente de cada sesion abierta del usuario: sin revocar y sin vencer.</summary>
    Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>El token vigente de una sesion, de cualquier usuario: el dueño lo verifica quien llama.</summary>
    Task<RefreshToken?> FindActiveBySessionAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task RevokeAllForUserExceptSessionAsync(
        Guid userId,
        Guid keptSessionId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default);
}

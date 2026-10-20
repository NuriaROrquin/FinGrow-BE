namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<bool> AnyIssuedAfterAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default);

    void Add(PasswordResetToken token);

    /// <summary>Marca como usados todos los enlaces del usuario que siguen sin usar, el que se acaba de
    /// canjear incluido.</summary>
    Task InvalidateAllForUserAsync(Guid userId, DateTimeOffset usedAt, CancellationToken cancellationToken = default);
}

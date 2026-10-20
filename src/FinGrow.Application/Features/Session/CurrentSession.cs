namespace FinGrow.Application.Features.Session;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;

/// <summary>
/// La sesion del pedido sale de la cookie de refresh, que el navegador manda a todo <c>/session</c>.
/// Solo cuenta si sigue vigente y es del usuario del access token: una cookie vieja o ajena no
/// marca ninguna sesion como actual.
/// </summary>
internal static class CurrentSession
{
    public static async Task<RefreshToken?> FindAsync(
        IRefreshTokenRepository refreshTokens,
        string? refreshToken,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var token = await refreshTokens.FindByHashAsync(RefreshToken.Hash(refreshToken), cancellationToken);

        return token is not null && token.UserId == userId && token.IsActive(now) ? token : null;
    }
}

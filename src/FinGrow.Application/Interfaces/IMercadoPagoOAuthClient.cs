namespace FinGrow.Application.Interfaces;

public sealed record MercadoPagoTokens(string AccessToken, string? RefreshToken, TimeSpan ExpiresIn, string UserId);

public interface IMercadoPagoOAuthClient
{
    Uri BuildAuthorizationUrl(string state);

    Task<MercadoPagoTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<MercadoPagoTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}

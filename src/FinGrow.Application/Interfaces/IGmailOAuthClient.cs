namespace FinGrow.Application.Interfaces;

public sealed record GmailTokens(string AccessToken, string? RefreshToken, TimeSpan ExpiresIn, bool CanReadMail);

public interface IGmailOAuthClient
{
    Uri BuildAuthorizationUrl(string state);

    Task<GmailTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<GmailTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<string> GetEmailAddressAsync(string accessToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(string token, CancellationToken cancellationToken = default);
}

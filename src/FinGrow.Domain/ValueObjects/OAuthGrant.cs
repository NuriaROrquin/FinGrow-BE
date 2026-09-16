namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

public sealed class OAuthGrant : ValueObject
{
    private OAuthGrant()
    {
    }

    private OAuthGrant(string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
    }

    public string AccessToken { get; private set; } = string.Empty;

    public string RefreshToken { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public static OAuthGrant From(string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new DomainException("El access token de la autorizacion es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new DomainException("El refresh token de la autorizacion es obligatorio.");
        }

        return new OAuthGrant(accessToken.Trim(), refreshToken.Trim(), expiresAt);
    }

    public bool ExpiresWithin(TimeSpan window, DateTimeOffset now) => ExpiresAt <= now.Add(window);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AccessToken;
        yield return RefreshToken;
        yield return ExpiresAt;
    }
}

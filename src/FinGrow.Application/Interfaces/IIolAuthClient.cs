namespace FinGrow.Application.Interfaces;

public sealed record IolTokens(string AccessToken, string RefreshToken, TimeSpan ExpiresIn);

/// <summary>
/// Canjea credenciales de InvertirOnline por tokens. Un rechazo de IOL llega como
/// <see cref="HttpRequestException"/> con <c>StatusCode</c> 400 o 401; cualquier otro fallo es de disponibilidad.
/// </summary>
public interface IIolAuthClient
{
    Task<IolTokens> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    Task<IolTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}

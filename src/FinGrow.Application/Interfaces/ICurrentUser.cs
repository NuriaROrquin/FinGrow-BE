namespace FinGrow.Application.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }

    Guid? CompanyId { get; }

    string? FullName { get; }

    string? Role { get; }

    /// <summary>La sesion (cadena de refresh tokens, HU-73) a la que pertenece el access token. Null en
    /// un token emitido antes de que el access token llevara su sesion, que vence solo en a lo sumo
    /// <c>Jwt:ExpirationMinutes</c>.</summary>
    Guid? SessionId { get; }

    DateTimeOffset? ExpiresAt { get; }

    bool IsAuthenticated { get; }
}

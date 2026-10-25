namespace FinGrow.Domain.Entities;

using System.Security.Cryptography;
using System.Text;
using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

public sealed class RefreshToken : AggregateRoot
{
    public const int HashLength = 64;
    public const int MaxRoleLength = 20;
    public const int MaxUserAgentLength = 512;

    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        Guid companyId,
        string role,
        string tokenHash,
        Guid sessionId,
        DateTimeOffset startedAt,
        string? userAgent,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        CompanyId = companyId;
        Role = role;
        TokenHash = tokenHash;
        SessionId = sessionId;
        StartedAt = startedAt;
        UserAgent = userAgent;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Identifica la sesion a traves de las rotaciones: cada refresh emite un token nuevo
    /// que hereda este id, asi que en una sesion abierta hay un solo token activo con este valor.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>Cuando se inicio sesion. <see cref="CreatedAt"/> es la ultima renovacion.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>El User-Agent del ultimo pedido que emitio el token; null si no vino ninguno.</summary>
    public string? UserAgent { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static RefreshToken Issue(
        Guid userId,
        Guid companyId,
        string role,
        string value,
        DateTimeOffset createdAt,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Un refresh token siempre pertenece a un usuario.");
        }

        if (companyId == Guid.Empty)
        {
            throw new DomainException("Un refresh token siempre pertenece a una empresa.");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new DomainException("Un refresh token siempre tiene un rol asociado.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("El valor del refresh token es obligatorio.");
        }

        var id = Guid.CreateVersion7();

        return new RefreshToken(
            id,
            userId,
            companyId,
            role,
            Hash(value),
            sessionId: id,
            startedAt: createdAt,
            NormalizeUserAgent(userAgent),
            createdAt,
            createdAt.Add(Lifetime));
    }

    /// <summary>Revoca este token y emite el siguiente de la misma sesion: conserva el id y el
    /// inicio de la sesion, y el User-Agent nuevo si vino uno (si no, el anterior).</summary>
    public RefreshToken Rotate(string value, DateTimeOffset now, string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("El valor del refresh token es obligatorio.");
        }

        Revoke(now);

        return new RefreshToken(
            Guid.CreateVersion7(),
            UserId,
            CompanyId,
            Role,
            Hash(value),
            SessionId,
            StartedAt,
            NormalizeUserAgent(userAgent) ?? UserAgent,
            now,
            now.Add(Lifetime));
    }

    /// <summary>Genera el valor opaco que se entrega al cliente (256 bits de entropia, base64url).
    /// No es un JWT: no lleva claims ni se puede decodificar, solo sirve como llave de busqueda
    /// por hash en la base.</summary>
    public static string GenerateValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    public static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)));

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    /// <summary>Idempotente a proposito: revocar dos veces (p.ej. un logout despues de un cambio
    /// de contrasena) no debe pisar el momento real de la primera revocacion.</summary>
    public void Revoke(DateTimeOffset revokedAt) => RevokedAt ??= revokedAt;

    private static string? NormalizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        var trimmed = userAgent.Trim();
        return trimmed.Length <= MaxUserAgentLength ? trimmed : trimmed[..MaxUserAgentLength];
    }
}

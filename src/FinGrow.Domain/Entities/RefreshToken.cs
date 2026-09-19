namespace FinGrow.Domain.Entities;

using System.Security.Cryptography;
using System.Text;
using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

public sealed class RefreshToken : AggregateRoot
{
    public const int HashLength = 64;
    public const int MaxRoleLength = 20;

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
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        CompanyId = companyId;
        Role = role;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static RefreshToken Issue(
        Guid userId,
        Guid companyId,
        string role,
        string value,
        DateTimeOffset createdAt)
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

        return new RefreshToken(
            Guid.CreateVersion7(),
            userId,
            companyId,
            role,
            Hash(value),
            createdAt,
            createdAt.Add(Lifetime));
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
}

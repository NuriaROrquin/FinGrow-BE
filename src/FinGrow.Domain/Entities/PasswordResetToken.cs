namespace FinGrow.Domain.Entities;

using System.Security.Cryptography;
using System.Text;
using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

/// <summary>
/// HU-72: el enlace de "olvidé mi contraseña". Como el refresh token, en la base queda solo el hash:
/// quien lea la tabla no puede cambiarle la contraseña a nadie.
/// </summary>
public sealed class PasswordResetToken : AggregateRoot
{
    public const int HashLength = 64;
    public const int MaxRoleLength = RefreshToken.MaxRoleLength;

    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private PasswordResetToken()
    {
    }

    private PasswordResetToken(
        Guid id,
        Guid userId,
        Guid companyId,
        string role,
        string tokenHash,
        DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        CompanyId = companyId;
        Role = role;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = createdAt.Add(Lifetime);
    }

    public Guid UserId { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public static PasswordResetToken Issue(
        Guid userId,
        Guid companyId,
        string role,
        string value,
        DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Un enlace de recuperacion siempre pertenece a un usuario.");
        }

        if (companyId == Guid.Empty)
        {
            throw new DomainException("Un enlace de recuperacion siempre pertenece a una empresa.");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new DomainException("Un enlace de recuperacion siempre tiene un rol asociado.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("El valor del enlace de recuperacion es obligatorio.");
        }

        return new PasswordResetToken(Guid.CreateVersion7(), userId, companyId, role, Hash(value), createdAt);
    }

    public static string GenerateValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    public static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)));

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && now < ExpiresAt;

    public void Redeem(DateTimeOffset now)
    {
        if (UsedAt is not null)
        {
            throw new DomainException("El enlace de recuperacion ya fue usado.");
        }

        if (now >= ExpiresAt)
        {
            throw new DomainException("El enlace de recuperacion vencio.");
        }

        UsedAt = now;
    }
}

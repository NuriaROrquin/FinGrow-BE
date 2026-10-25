namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public sealed class User : AggregateRoot
{
    public const int MaxPasswordHashLength = 200;

    private User()
    {
    }

    private User(Guid id, Email email, string passwordHash, UserRole role, DateTimeOffset createdAt)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Email Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public string? TwoFactorSecret { get; private set; }

    public DateTimeOffset? TwoFactorEnabledAt { get; private set; }

    public bool IsTwoFactorEnabled => TwoFactorEnabledAt is not null;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    internal static User Create(Guid id, Email email, string passwordHash, UserRole role, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (id == Guid.Empty)
        {
            throw new DomainException("Un usuario siempre tiene un id.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainException($"El rol '{role}' no es un valor valido.");
        }

        return new User(id, email, EnsureHash(passwordHash), role, createdAt);
    }

    public void ChangeEmail(Email email, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(email);

        Email = email;
        UpdatedAt = updatedAt;
    }

    public void ChangePassword(string passwordHash, DateTimeOffset updatedAt)
    {
        PasswordHash = EnsureHash(passwordHash);
        UpdatedAt = updatedAt;
    }

    public void RegisterLogin(DateTimeOffset loggedInAt)
    {
        if (!IsActive)
        {
            throw new DomainException("La cuenta esta desactivada y no puede iniciar sesion.");
        }

        LastLoginAt = loggedInAt;
    }

    public void StartTwoFactorEnrollment(string secret, DateTimeOffset updatedAt)
    {
        if (IsTwoFactorEnabled)
        {
            throw new DomainException("El doble factor ya esta activo.");
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new DomainException("El secreto del doble factor es obligatorio.");
        }

        TwoFactorSecret = secret;
        UpdatedAt = updatedAt;
    }

    public void EnableTwoFactor(DateTimeOffset enabledAt)
    {
        if (IsTwoFactorEnabled)
        {
            throw new DomainException("El doble factor ya esta activo.");
        }

        if (TwoFactorSecret is null)
        {
            throw new DomainException("No se puede activar el doble factor sin haber iniciado el alta.");
        }

        TwoFactorEnabledAt = enabledAt;
        UpdatedAt = enabledAt;
    }

    public void DisableTwoFactor(DateTimeOffset updatedAt)
    {
        TwoFactorSecret = null;
        TwoFactorEnabledAt = null;
        UpdatedAt = updatedAt;
    }

    internal void Deactivate(DateTimeOffset updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }

    internal void Activate(DateTimeOffset updatedAt)
    {
        IsActive = true;
        UpdatedAt = updatedAt;
    }

    private static string EnsureHash(string passwordHash) =>
        string.IsNullOrWhiteSpace(passwordHash)
            ? throw new DomainException("La contrasena es obligatoria.")
            : passwordHash;
}

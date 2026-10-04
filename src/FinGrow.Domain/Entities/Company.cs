namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// La empresa contratante del beneficio. Es la raiz del aislamiento de datos: todo lo que
/// ve un panel empresarial cuelga de un CompanyId (ver T-10).
/// </summary>
public sealed class Company : AggregateRoot, ITwoFactorAccount
{
    public const int MaxNameLength = 200;

    private readonly List<Department> _departments = new();

    private Company()
    {
    }

    private Company(
        Guid id,
        string name,
        TaxId taxId,
        Email email,
        string passwordHash,
        Currency defaultCurrency,
        DateTimeOffset createdAt)
        : base(id)
    {
        Name = name;
        TaxId = taxId;
        Email = email;
        PasswordHash = passwordHash;
        DefaultCurrency = defaultCurrency;
        IsActive = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>Razon social.</summary>
    public string Name { get; private set; } = string.Empty;

    public TaxId TaxId { get; private set; } = null!;

    public Email Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = string.Empty;

    public Currency DefaultCurrency { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? TwoFactorSecret { get; private set; }

    public DateTimeOffset? TwoFactorEnabledAt { get; private set; }

    public bool IsTwoFactorEnabled => TwoFactorEnabledAt is not null;

    public IReadOnlyCollection<Department> Departments => _departments.AsReadOnly();

    public static Company Create(
        string name,
        TaxId taxId,
        Email email,
        string passwordHash,
        Currency defaultCurrency,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(taxId);
        ArgumentNullException.ThrowIfNull(email);

        return new Company(
            Guid.CreateVersion7(),
            EnsureValidName(name),
            taxId,
            email,
            EnsureHash(passwordHash),
            defaultCurrency,
            createdAt);
    }

    public Department AddDepartment(string name, string? description, string? managerName, DateTimeOffset createdAt)
    {
        if (_departments.Any(department => department.HasName(name)))
        {
            throw new DomainException($"Ya existe un departamento llamado '{name}'.");
        }

        var department = Department.Create(Id, name, description, managerName, createdAt);
        _departments.Add(department);
        UpdatedAt = createdAt;

        return department;
    }

    public void UpdateProfile(string name, Email email, Currency defaultCurrency, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(email);

        Name = EnsureValidName(name);
        Email = email;
        DefaultCurrency = defaultCurrency;
        UpdatedAt = updatedAt;
    }

    public void ChangePassword(string passwordHash, DateTimeOffset updatedAt)
    {
        PasswordHash = EnsureHash(passwordHash);
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTimeOffset updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }

    public void Activate(DateTimeOffset updatedAt)
    {
        IsActive = true;
        UpdatedAt = updatedAt;
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

    private static string EnsureValidName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("La razon social es obligatoria."),
            > MaxNameLength => throw new DomainException(
                $"La razon social no puede superar los {MaxNameLength} caracteres."),
            _ => trimmed
        };
    }

    private static string EnsureHash(string passwordHash) =>
        string.IsNullOrWhiteSpace(passwordHash)
            ? throw new DomainException("La contrasena es obligatoria.")
            : passwordHash;
}

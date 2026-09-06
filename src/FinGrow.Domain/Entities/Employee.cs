namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// La persona que usa la app. Es raiz de agregado porque tiene ciclo de vida propio
/// (credenciales, preferencias, baja logica) aunque pertenezca a una empresa.
/// </summary>
public sealed class Employee : AggregateRoot
{
    public const int MaxFullNameLength = 200;
    public const int MaxPhoneNumberLength = 30;

    private Employee()
    {
    }

    private Employee(
        Guid id,
        Guid companyId,
        Guid? departmentId,
        string fullName,
        Email email,
        string? phoneNumber,
        string passwordHash,
        Currency preferredCurrency,
        DateOnly hiredOn,
        DateTimeOffset createdAt)
        : base(id)
    {
        CompanyId = companyId;
        DepartmentId = departmentId;
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        PasswordHash = passwordHash;
        PreferredCurrency = preferredCurrency;
        HiredOn = hiredOn;
        IsActive = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Opcional a proposito: un alta puede existir antes de que la empresa termine de armar su
    /// organigrama. La relacion es por id y no por nombre como en los mocks del frontend, asi
    /// renombrar un departamento no toca a ningun empleado.
    /// </summary>
    public Guid? DepartmentId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public Email Email { get; private set; } = null!;

    public string? PhoneNumber { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public Currency PreferredCurrency { get; private set; }

    public DateOnly HiredOn { get; private set; }

    /// <summary>Baja logica: desactivar corta el acceso sin borrar el historial financiero (HU-55).</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Company Company { get; private set; } = null!;

    public Department? Department { get; private set; }

    public static Employee Create(
        Guid companyId,
        Guid? departmentId,
        string fullName,
        Email email,
        string? phoneNumber,
        string passwordHash,
        Currency preferredCurrency,
        DateOnly hiredOn,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (companyId == Guid.Empty)
        {
            throw new DomainException("Un empleado siempre pertenece a una empresa.");
        }

        return new Employee(
            Guid.CreateVersion7(),
            companyId,
            departmentId,
            EnsureValidFullName(fullName),
            email,
            EnsureValidPhoneNumber(phoneNumber),
            EnsureHash(passwordHash),
            preferredCurrency,
            hiredOn,
            createdAt);
    }

    public void UpdateProfile(string fullName, string? phoneNumber, Currency preferredCurrency, DateTimeOffset updatedAt)
    {
        FullName = EnsureValidFullName(fullName);
        PhoneNumber = EnsureValidPhoneNumber(phoneNumber);
        PreferredCurrency = preferredCurrency;
        UpdatedAt = updatedAt;
    }

    public void AssignToDepartment(Guid? departmentId, DateTimeOffset updatedAt)
    {
        DepartmentId = departmentId;
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
            throw new DomainException("El empleado esta dado de baja y no puede iniciar sesion.");
        }

        LastLoginAt = loggedInAt;
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

    private static string EnsureValidFullName(string fullName)
    {
        var trimmed = (fullName ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("El nombre del empleado es obligatorio."),
            > MaxFullNameLength => throw new DomainException(
                $"El nombre no puede superar los {MaxFullNameLength} caracteres."),
            _ => trimmed
        };
    }

    private static string? EnsureValidPhoneNumber(string? phoneNumber)
    {
        var trimmed = phoneNumber?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaxPhoneNumberLength
            ? throw new DomainException($"El telefono no puede superar los {MaxPhoneNumberLength} caracteres.")
            : trimmed;
    }

    private static string EnsureHash(string passwordHash) =>
        string.IsNullOrWhiteSpace(passwordHash)
            ? throw new DomainException("La contrasena es obligatoria.")
            : passwordHash;
}

namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public sealed class Employee : AggregateRoot, ITwoFactorAccount
{
    public const int MinFullNameLength = 2;
    public const int MaxFullNameLength = 200;
    public const int MaxPhoneNumberLength = 30;
    public const int MinPhoneNumberDigits = 8;

    /// <summary>El maximo de digitos de un numero internacional (E.164).</summary>
    public const int MaxPhoneNumberDigits = 15;

    public const int MinNationalIdLength = 7;
    public const int MaxNationalIdLength = 8;
    public const int MaxAddressLength = 200;

    /// <summary>La edad minima para trabajar en Argentina (Ley 26.390).</summary>
    public const int MinAge = 16;

    public const int MaxAge = 100;

    private const string PhoneNumberSeparators = " +-()";

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
        Theme = Theme.System;
        Language = Language.es;
        DateFormat = DateFormat.DayMonthYear;
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

    public string? NationalId { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public string? Address { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public Currency PreferredCurrency { get; private set; }

    public Theme Theme { get; private set; }

    public Language Language { get; private set; }

    public DateFormat DateFormat { get; private set; }

    public DateOnly HiredOn { get; private set; }

    /// <summary>Baja logica: desactivar corta el acceso sin borrar el historial financiero (HU-55).</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public string? TwoFactorSecret { get; private set; }

    public DateTimeOffset? TwoFactorEnabledAt { get; private set; }

    public bool IsTwoFactorEnabled => TwoFactorEnabledAt is not null;

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

    public void UpdateProfile(
        string fullName,
        string? phoneNumber,
        string? nationalId,
        DateOnly? birthDate,
        string? address,
        DateTimeOffset updatedAt)
    {
        FullName = EnsureValidFullName(fullName);
        PhoneNumber = EnsureValidPhoneNumber(phoneNumber);
        NationalId = EnsureValidNationalId(nationalId);
        BirthDate = EnsureValidBirthDate(birthDate, DateOnly.FromDateTime(updatedAt.Date));
        Address = EnsureValidAddress(address);
        UpdatedAt = updatedAt;
    }

    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        var digits = phoneNumber.Count(char.IsAsciiDigit);

        return phoneNumber.Length <= MaxPhoneNumberLength
            && phoneNumber.All(character => char.IsAsciiDigit(character) || PhoneNumberSeparators.Contains(character))
            && digits is >= MinPhoneNumberDigits and <= MaxPhoneNumberDigits;
    }

    public static bool IsValidNationalId(string nationalId) =>
        nationalId.Length is >= MinNationalIdLength and <= MaxNationalIdLength
        && nationalId.All(char.IsAsciiDigit);

    public static bool IsValidBirthDate(DateOnly birthDate, DateOnly today) =>
        birthDate <= today.AddYears(-MinAge) && birthDate > today.AddYears(-(MaxAge + 1));

    public void UpdatePreferences(
        Theme theme,
        Language language,
        Currency preferredCurrency,
        DateFormat dateFormat,
        DateTimeOffset updatedAt)
    {
        PreferredCurrency = EnsureDefined(preferredCurrency, "La moneda");
        Theme = EnsureDefined(theme, "El tema");
        Language = EnsureDefined(language, "El idioma");
        DateFormat = EnsureDefined(dateFormat, "El formato de fecha");
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

    public void DisableTwoFactor(DateTimeOffset updatedAt)
    {
        TwoFactorSecret = null;
        TwoFactorEnabledAt = null;
        UpdatedAt = updatedAt;
    }

    private static string EnsureValidFullName(string fullName)
    {
        var trimmed = (fullName ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("El nombre del empleado es obligatorio."),
            < MinFullNameLength => throw new DomainException(
                $"El nombre tiene que tener al menos {MinFullNameLength} caracteres."),
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

        return IsValidPhoneNumber(trimmed)
            ? trimmed
            : throw new DomainException(
                $"El telefono solo admite numeros, espacios, +, - y parentesis, con entre {MinPhoneNumberDigits} y {MaxPhoneNumberDigits} digitos.");
    }

    private static string? EnsureValidNationalId(string? nationalId)
    {
        var trimmed = nationalId?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return IsValidNationalId(trimmed)
            ? trimmed
            : throw new DomainException(
                $"El DNI tiene que tener entre {MinNationalIdLength} y {MaxNationalIdLength} numeros, sin puntos.");
    }

    private static DateOnly? EnsureValidBirthDate(DateOnly? birthDate, DateOnly today) =>
        birthDate is not { } date || IsValidBirthDate(date, today)
            ? birthDate
            : throw new DomainException($"La fecha de nacimiento tiene que corresponder a una edad de entre {MinAge} y {MaxAge} años.");

    private static string? EnsureValidAddress(string? address)
    {
        var trimmed = address?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaxAddressLength
            ? throw new DomainException($"La direccion no puede superar los {MaxAddressLength} caracteres.")
            : trimmed;
    }

    private static TEnum EnsureDefined<TEnum>(TEnum value, string preference)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value)
            ? value
            : throw new DomainException($"{preference} '{value}' no es un valor valido.");

    private static string EnsureHash(string passwordHash) =>
        string.IsNullOrWhiteSpace(passwordHash)
            ? throw new DomainException("La contrasena es obligatoria.")
            : passwordHash;
}

namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public sealed class Employee : AggregateRoot
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

    private readonly List<EmployeeDepartmentAssignment> _departmentAssignments = new();

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
        User = User.Create(id, email, passwordHash, UserRole.Employee, createdAt);
        PhoneNumber = phoneNumber;
        PreferredCurrency = preferredCurrency;
        Theme = Theme.System;
        Language = Language.es;
        DateFormat = DateFormat.DayMonthYear;
        HiredOn = hiredOn;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;

        if (departmentId is { } assignedDepartmentId)
        {
            _departmentAssignments.Add(EmployeeDepartmentAssignment.Start(id, assignedDepartmentId, createdAt));
        }
    }

    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Opcional a proposito: un alta puede existir antes de que la empresa termine de armar su
    /// organigrama. La relacion es por id y no por nombre como en los mocks del frontend, asi
    /// renombrar un departamento no toca a ningun empleado.
    /// </summary>
    public Guid? DepartmentId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? NationalId { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public string? Address { get; private set; }

    public Currency PreferredCurrency { get; private set; }

    public Theme Theme { get; private set; }

    public Language Language { get; private set; }

    public DateFormat DateFormat { get; private set; }

    public DateOnly HiredOn { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public User User { get; private set; } = null!;

    public Company Company { get; private set; } = null!;

    public Department? Department { get; private set; }

    public IReadOnlyCollection<EmployeeDepartmentAssignment> DepartmentAssignments => _departmentAssignments.AsReadOnly();

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
            passwordHash,
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

    public void UpdateDetails(string fullName, Email email, string? phoneNumber, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(email);

        FullName = EnsureValidFullName(fullName);
        User.ChangeEmail(email, updatedAt);
        PhoneNumber = EnsureValidPhoneNumber(phoneNumber);
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

    public void AssignToDepartment(Guid? departmentId, DateTimeOffset assignedAt)
    {
        if (departmentId == DepartmentId)
        {
            return;
        }

        _departmentAssignments.SingleOrDefault(assignment => assignment.IsCurrent)?.End(assignedAt);

        if (departmentId is { } newDepartmentId)
        {
            _departmentAssignments.Add(EmployeeDepartmentAssignment.Start(Id, newDepartmentId, assignedAt));
        }

        DepartmentId = departmentId;
        UpdatedAt = assignedAt;
    }

    public void Deactivate(DateTimeOffset updatedAt)
    {
        User.Deactivate(updatedAt);
        UpdatedAt = updatedAt;
    }

    public void Activate(DateTimeOffset updatedAt)
    {
        User.Activate(updatedAt);
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
}

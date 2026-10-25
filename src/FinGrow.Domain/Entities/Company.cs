namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// La empresa contratante del beneficio. Es la raiz del aislamiento de datos: todo lo que
/// ve un panel empresarial cuelga de un CompanyId (ver T-10).
/// </summary>
public sealed class Company : AggregateRoot
{
    public const int MaxNameLength = 200;
    public const int MaxIndustryLength = 100;
    public const int MaxPhoneNumberLength = 30;
    public const int MaxFiscalAddressLength = 300;

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
        User = User.Create(id, email, passwordHash, UserRole.Company, createdAt);
        DefaultCurrency = defaultCurrency;
        Theme = Theme.Light;
        Language = Language.es;
        FiscalYearStartMonth = 1;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>Razon social.</summary>
    public string Name { get; private set; } = string.Empty;

    public TaxId TaxId { get; private set; } = null!;

    public User User { get; private set; } = null!;

    public string? Industry { get; private set; }

    public string? PhoneNumber { get; private set; }

    public string? FiscalAddress { get; private set; }

    public int? FoundedYear { get; private set; }

    public Currency DefaultCurrency { get; private set; }

    public Theme Theme { get; private set; }

    public Language Language { get; private set; }

    public int FiscalYearStartMonth { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

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
            passwordHash,
            defaultCurrency,
            createdAt);
    }

    public Department AddDepartment(string name, string? description, Guid? managerId, DateTimeOffset createdAt)
    {
        if (IsDepartmentNameTaken(name))
        {
            throw new DomainException($"Ya existe un departamento llamado '{name}'.");
        }

        var department = Department.Create(Id, name, description, managerId, createdAt);
        _departments.Add(department);
        UpdatedAt = createdAt;

        return department;
    }

    public Department? FindDepartment(Guid departmentId) =>
        _departments.SingleOrDefault(department => department.Id == departmentId);

    public bool IsDepartmentNameTaken(string name, Guid? excludingDepartmentId = null) =>
        _departments.Any(department => department.Id != excludingDepartmentId && department.HasName(name));

    public Department UpdateDepartment(
        Guid departmentId,
        string name,
        string? description,
        Guid? managerId,
        DateTimeOffset updatedAt)
    {
        var department = GetDepartment(departmentId);

        if (IsDepartmentNameTaken(name, departmentId))
        {
            throw new DomainException($"Ya existe un departamento llamado '{name}'.");
        }

        department.Update(name, description, managerId, updatedAt);
        UpdatedAt = updatedAt;

        return department;
    }

    public void RemoveDepartment(Guid departmentId, DateTimeOffset removedAt)
    {
        _departments.Remove(GetDepartment(departmentId));
        UpdatedAt = removedAt;
    }

    public Department DeactivateDepartment(Guid departmentId, DateTimeOffset deactivatedAt)
    {
        var department = GetDepartment(departmentId);
        department.Deactivate(deactivatedAt);
        UpdatedAt = deactivatedAt;

        return department;
    }

    public Department ActivateDepartment(Guid departmentId, DateTimeOffset activatedAt)
    {
        var department = GetDepartment(departmentId);
        department.Activate(activatedAt);
        UpdatedAt = activatedAt;

        return department;
    }

    public void UpdateProfile(string name, Email email, Currency defaultCurrency, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(email);

        Name = EnsureValidName(name);
        User.ChangeEmail(email, updatedAt);
        DefaultCurrency = defaultCurrency;
        UpdatedAt = updatedAt;
    }

    public void UpdatePreferences(
        Theme theme,
        Language language,
        Currency currency,
        int fiscalYearStartMonth,
        DateTimeOffset updatedAt)
    {
        Theme = EnsureDefined(theme, "El tema");
        Language = EnsureDefined(language, "El idioma");
        DefaultCurrency = EnsureDefined(currency, "La moneda");

        if (fiscalYearStartMonth is < 1 or > 12)
        {
            throw new DomainException("El mes de inicio del año fiscal tiene que estar entre 1 y 12.");
        }

        FiscalYearStartMonth = fiscalYearStartMonth;
        UpdatedAt = updatedAt;
    }

    public void UpdateCorporateProfile(string name, TaxId taxId, Email email, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(taxId);
        ArgumentNullException.ThrowIfNull(email);

        Name = EnsureValidName(name);
        TaxId = taxId;
        User.ChangeEmail(email, updatedAt);
        UpdatedAt = updatedAt;
    }

    public void UpdateCorporateInformation(
        string name,
        TaxId taxId,
        Email email,
        string? industry,
        string? phoneNumber,
        string? fiscalAddress,
        int? foundedYear,
        DateTimeOffset updatedAt)
    {
        UpdateCorporateProfile(name, taxId, email, updatedAt);
        Industry = EnsureIndustry(industry);
        PhoneNumber = EnsureOptionalText(phoneNumber, MaxPhoneNumberLength, "El teléfono");
        FiscalAddress = EnsureOptionalText(fiscalAddress, MaxFiscalAddressLength, "La dirección fiscal");

        if (foundedYear is < 1800 or > 9999)
        {
            throw new DomainException("El año de fundación no es válido.");
        }

        FoundedYear = foundedYear;
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

    private Department GetDepartment(Guid departmentId) =>
        FindDepartment(departmentId)
        ?? throw new DomainException("El departamento no pertenece a esta empresa.");

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

    private static TEnum EnsureDefined<TEnum>(TEnum value, string fieldName)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value)
            ? value
            : throw new DomainException($"{fieldName} no es valido.");

    private static string? EnsureOptionalText(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{fieldName} no puede superar los {maxLength} caracteres.");
        }

        return trimmed;
    }

    private static string? EnsureIndustry(string? value)
    {
        var industry = EnsureOptionalText(value, MaxIndustryLength, "La industria");

        return industry is null || IndustryTypes.IsValid(industry)
            ? industry
            : throw new DomainException("La industria seleccionada no es válida.");
    }
}

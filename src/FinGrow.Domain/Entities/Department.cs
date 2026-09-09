namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

/// <summary>
/// Un departamento vive dentro de una empresa y no tiene sentido por fuera de ella: por eso
/// es una <see cref="Entity"/> del agregado Company y no una raiz propia.
/// </summary>
public sealed class Department : Entity
{
    public const int MaxNameLength = 120;
    public const int MaxDescriptionLength = 500;
    public const int MaxManagerNameLength = 150;

    private Department()
    {
    }

    private Department(
        Guid id,
        Guid companyId,
        string name,
        string? description,
        string? managerName,
        DateTimeOffset createdAt)
        : base(id)
    {
        CompanyId = companyId;
        Name = name;
        Description = description;
        ManagerName = managerName;
        IsActive = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>Responsable a cargo. Texto libre: hoy no es necesariamente un empleado de la plataforma.</summary>
    public string? ManagerName { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    internal static Department Create(
        Guid companyId,
        string name,
        string? description,
        string? managerName,
        DateTimeOffset createdAt) =>
        new(Guid.CreateVersion7(),
            companyId,
            EnsureValidName(name),
            Truncated(description, MaxDescriptionLength, "La descripcion"),
            Truncated(managerName, MaxManagerNameLength, "El nombre del responsable"),
            createdAt);

    public bool HasName(string name) =>
        string.Equals(Name, (name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

    public void Update(string name, string? description, string? managerName, DateTimeOffset updatedAt)
    {
        Name = EnsureValidName(name);
        Description = Truncated(description, MaxDescriptionLength, "La descripcion");
        ManagerName = Truncated(managerName, MaxManagerNameLength, "El nombre del responsable");
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Desactivar en lugar de borrar: una reestructuracion no puede llevarse puesto el historico
    /// de los empleados que pasaron por el departamento (HU-53).
    /// </summary>
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

    private static string EnsureValidName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();

        return trimmed.Length switch
        {
            0 => throw new DomainException("El nombre del departamento es obligatorio."),
            > MaxNameLength => throw new DomainException(
                $"El nombre del departamento no puede superar los {MaxNameLength} caracteres."),
            _ => trimmed
        };
    }

    private static string? Truncated(string? value, int maxLength, string label)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > maxLength
            ? throw new DomainException($"{label} no puede superar los {maxLength} caracteres.")
            : trimmed;
    }
}

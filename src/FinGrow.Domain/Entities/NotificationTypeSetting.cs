namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class NotificationTypeSetting : AggregateRoot
{
    private NotificationTypeSetting()
    {
    }

    private NotificationTypeSetting(
        Guid id,
        Guid employeeId,
        NotificationType type,
        bool isEnabled,
        DateTimeOffset updatedAt)
        : base(id)
    {
        EmployeeId = employeeId;
        Type = type;
        IsEnabled = isEnabled;
        UpdatedAt = updatedAt;
    }

    public Guid EmployeeId { get; private set; }

    public NotificationType Type { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static NotificationTypeSetting Create(
        Guid employeeId,
        NotificationType type,
        bool isEnabled,
        DateTimeOffset updatedAt)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("La preferencia de un tipo de alerta siempre pertenece a un empleado.");
        }

        return Enum.IsDefined(type)
            ? new NotificationTypeSetting(Guid.CreateVersion7(), employeeId, type, isEnabled, updatedAt)
            : throw new DomainException($"El tipo de alerta '{type}' no existe.");
    }

    public void Change(bool isEnabled, DateTimeOffset updatedAt)
    {
        IsEnabled = isEnabled;
        UpdatedAt = updatedAt;
    }
}

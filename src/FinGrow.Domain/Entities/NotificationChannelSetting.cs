namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class NotificationChannelSetting : AggregateRoot
{
    private NotificationChannelSetting()
    {
    }

    private NotificationChannelSetting(
        Guid id,
        Guid employeeId,
        NotificationChannel channel,
        bool isEnabled,
        DateTimeOffset updatedAt)
        : base(id)
    {
        EmployeeId = employeeId;
        Channel = channel;
        IsEnabled = isEnabled;
        UpdatedAt = updatedAt;
    }

    public Guid EmployeeId { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static NotificationChannelSetting Create(
        Guid employeeId,
        NotificationChannel channel,
        bool isEnabled,
        DateTimeOffset updatedAt)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("La preferencia de un canal siempre pertenece a un empleado.");
        }

        if (!Enum.IsDefined(channel))
        {
            throw new DomainException($"El canal '{channel}' no existe.");
        }

        return channel.IsConfigurable()
            ? new NotificationChannelSetting(Guid.CreateVersion7(), employeeId, channel, isEnabled, updatedAt)
            : throw new DomainException("Las notificaciones de la app no se pueden desactivar: son el historial de todas las alertas.");
    }

    public void Change(bool isEnabled, DateTimeOffset updatedAt)
    {
        IsEnabled = isEnabled;
        UpdatedAt = updatedAt;
    }
}

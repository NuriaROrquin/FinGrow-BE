namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class NotificationRecipient : ValueObject
{
    private NotificationRecipient(NotificationRecipientType type, Guid id)
    {
        Type = type;
        Id = id;
    }

    public NotificationRecipientType Type { get; }

    public Guid Id { get; }

    public static NotificationRecipient Employee(Guid employeeId) => From(NotificationRecipientType.Employee, employeeId);

    public static NotificationRecipient Company(Guid companyId) => From(NotificationRecipientType.Company, companyId);

    public static NotificationRecipient From(NotificationRecipientType type, Guid id)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException($"El tipo de destinatario '{type}' no existe.");
        }

        return id == Guid.Empty
            ? throw new DomainException("Una notificacion siempre tiene un destinatario.")
            : new NotificationRecipient(type, id);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Id;
    }
}

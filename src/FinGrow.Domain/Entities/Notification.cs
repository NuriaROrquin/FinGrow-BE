namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public sealed class Notification : AggregateRoot
{
    public const int MaxTitleLength = 150;
    public const int MaxBodyLength = 1000;
    public const int MaxDeduplicationKeyLength = 200;

    private Notification()
    {
    }

    private Notification(
        Guid id,
        NotificationRecipient recipient,
        NotificationType type,
        string title,
        string body,
        string deduplicationKey,
        DateTimeOffset createdAt)
        : base(id)
    {
        RecipientType = recipient.Type;
        RecipientId = recipient.Id;
        Type = type;
        Title = title;
        Body = body;
        DeduplicationKey = deduplicationKey;
        CreatedAt = createdAt;
    }

    public NotificationRecipientType RecipientType { get; private set; }

    public Guid RecipientId { get; private set; }

    public NotificationRecipient Recipient => NotificationRecipient.From(RecipientType, RecipientId);

    public NotificationType Type { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public string DeduplicationKey { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    public static Notification Create(
        NotificationRecipient recipient,
        NotificationType type,
        string title,
        string body,
        string deduplicationKey,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        if (!Enum.IsDefined(type))
        {
            throw new DomainException($"El tipo de notificacion '{type}' no existe.");
        }

        return new Notification(
            Guid.CreateVersion7(),
            recipient,
            type,
            RequiredText.Ensure(title, MaxTitleLength, "El titulo de la notificacion"),
            RequiredText.Ensure(body, MaxBodyLength, "El texto de la notificacion"),
            RequiredText.Ensure(deduplicationKey, MaxDeduplicationKeyLength, "El criterio de deduplicacion"),
            createdAt);
    }

    public bool IsAddressedTo(NotificationRecipient recipient) =>
        recipient is not null && recipient.Type == RecipientType && recipient.Id == RecipientId;

    public void MarkAsRead(DateTimeOffset readAt)
    {
        if (IsRead)
        {
            return;
        }

        if (readAt < CreatedAt)
        {
            throw new DomainException("Una notificacion no puede leerse antes de haberse creado.");
        }

        ReadAt = readAt;
    }
}
